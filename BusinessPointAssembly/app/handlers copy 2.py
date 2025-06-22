from aiogram import F, Router, Bot
from aiogram.filters import CommandStart
from aiogram.types import Message, CallbackQuery, InputMediaPhoto, InputMediaVideo
from aiogram.fsm.state import StatesGroup, State
from aiogram.fsm.context import FSMContext
from aiogram.enums import ParseMode
from aiogram.exceptions import TelegramUnauthorizedError, TelegramNotFound

import database.db_utils as db
import app.keyboards as kb
import traceback
import re

router = Router()

class Order(StatesGroup):
    token = State()
    questions = State()
    confirmation = State()

class Setup(StatesGroup):
    questions = State()
    confirmation = State()
    
@router.message(CommandStart())
async def Start(message: Message):
    user = await db.IsUser(message.chat.id)
    if user == None:
        await db.AddUser(message.chat.id, message.chat.username)
        print(message.chat.username)
    if user:
        print('User exists and banned')
    else:
        await message.answer_photo(
            caption="""<b>Привет, это BusinessPoint.</b>
            <blockquote>Универсальный инструмент для создания адаптивных CRM систем для владельцев малого бизнеса в Telegram!</blockquote>""",
            parse_mode=ParseMode.HTML,
            photo=await db.GetMedia("MainPhoto"),
            reply_markup=await kb.StartMenu())

@router.callback_query(F.data == 'list_bot')
async def ShowBotList(callback: CallbackQuery):
    await callback.message.edit_reply_markup(reply_markup=await kb.BuildBots(await db.GetBots(callback.from_user.id)))

@router.callback_query(F.data == 'botback')
async def BotBack(callback: CallbackQuery):
    file = InputMediaPhoto(media=await db.GetMedia("MainPhoto"), 
        caption="""<b>Привет, это BusinessPoint.</b>
            <blockquote>Универсальный инструмент для создания адаптивных CRM систем для владельцев малого бизнеса в Telegram!</blockquote>""",
        parse_mode=ParseMode.HTML)
    await callback.message.edit_media(media=file,
                                        reply_markup=await kb.StartMenu())

@router.callback_query(F.data.startswith('bot_'))
async def BotInfo(callback: CallbackQuery):
    bot = await db.GetBotByToken(callback.data[4:])
    file = InputMediaPhoto(media=await db.GetMedia("MainPhoto"), 
        caption=f"""<b>@{bot.Name}</b>
        <blockquote>Тут можно конфигурировать вашего бота.</blockquote>""",
        parse_mode=ParseMode.HTML)
    await callback.message.edit_media(media=file,
                                        reply_markup=await kb.ConfigureBot())

@router.callback_query(F.data == 'startcreate')
async def CreateBot(callback: CallbackQuery):
    file = InputMediaPhoto(media=await db.GetMedia("MainPhoto"), 
        caption="""<b>Создание бота.</b>
        <blockquote>Выберите тип бота и следуйте дальнейшим инструкциям.</blockquote>""",
        parse_mode=ParseMode.HTML)
    await callback.message.edit_media(media=file,
                                        reply_markup=await kb.BotCreateList())

@router.callback_query(F.data.startswith('create_FAQ'))
async def RoundOrderStart(callback: CallbackQuery, state: FSMContext):
    if await db.GetFillingStatus(callback.from_user.id) == False:
        await StartOrder(callback, state)
    else:
        await callback.bot.answer_callback_query(text='Вы уже создаете бота', 
                                                 callback_query_id=callback.id, show_alert=True)

async def StartOrder(callback: CallbackQuery, state: FSMContext):
    if callback.data.startswith("create_FAQ"):
        await db.UpdateFillingStatus(callback.from_user.id)
        file = InputMediaPhoto(media=await db.GetMedia("InstructionPhoto"), 
        caption="""<b>Процесс пошел.</b>
        <blockquote>Первый этап.</blockquote>
        <blockquote>Необходимо создать бота в @BotFather\nНазначить ему имя, ссылку, <i>фото, описание(не обязательно)</i></blockquote>
        <blockquote><b>Инструкция.</b>\n/start запускаем @BotFather\n/newbot команда для создания нового бота\nУказываем имя бота\nУказываем ссылку для бота(<i>имя</i>_bot)\nКопируем token и пересылаем в <b>BusinessPoint</b>\n\nДополнительно по команде /mybots можно настроить:\nФото для бота\nОписание бота</blockquote>
        <blockquote>Ждем ваш токен. Он выглядит так:\n<code>числа:буквы</code></blockquote>""",
        parse_mode=ParseMode.HTML)
        await callback.message.edit_media(media=file)
        await state.set_state(Order.token)

@router.message(F.content_type.in_({'text'}), Order.token)
async def GetMusic(message: Message, state: FSMContext):
    try:
        data = await state.get_data()
        if not data.get("token"):
            await state.update_data(token=message.text)
            await message.answer(
                text=
                f"""<blockquote>Ваш токен:\n<code>{message.text}</code></blockquote>""",
                parse_mode=ParseMode.HTML,
                reply_markup=await kb.ConfirmHash()
            )
    except Exception as e:
        await state.clear()
        #await ExceptionMessage(message, e)

@router.callback_query(lambda c: c.data in ["token_confirm", "token_edit"])
async def ConfirmMusic(callback: CallbackQuery, state: FSMContext):
    try:
        if callback.data == "token_confirm":
            data = await state.get_data()
            bot = Bot(data["token"])
            try:
                user = await bot.get_me()
                await db.UpdateFillingStatus(callback.from_user.id)
                await db.AddBot(callback.from_user.id, data["token"], user.username)
                file = InputMediaPhoto(media=await db.GetMedia("BotCreated"), 
                caption=f"""<b>Бот @{user.username} создан!</b>
                <blockquote>Второй этап.</blockquote>
                <blockquote>Теперь нужно задать вопросы и ответы к вашему боту.</blockquote>
                <blockquote>Перейдите в <b>список</b> ботов.</blockquote>""",
                parse_mode=ParseMode.HTML)
                await state.clear()
                await callback.message.edit_media(media=file, reply_markup=await kb.BotList())
            except TelegramUnauthorizedError or TelegramNotFound:
                await state.update_data(token=None)
                file = InputMediaPhoto(media=await db.GetMedia("InstructionPhoto"), 
                caption="""<b>Процесс пошел.</b>
                <blockquote>Первый этап.</blockquote>
                <blockquote>Необходимо создать бота в @BotFather\nНазначить ему имя, ссылку(обязательно)\n<i>Фото, описание(не обязательно)</i></blockquote>
                <blockquote><b>Инструкция.</b>\n/start запускаем @BotFather\n/newbot команда для создания нового бота\nУказываем имя бота\nУказываем ссылку для бота(<i>имя</i>_bot)\nКопируем token и пересылаем в <b>BusinessPoint</b>\n\nДополнительно по команде /mybots можно настроить:\nФото для бота\nОписание бота</blockquote>
                <blockquote>Ждем ваш токен. Он выглядит так:\n<code>числа:буквы</code></blockquote>""",
                parse_mode=ParseMode.HTML)
                await callback.message.edit_media(media=file)
                await state.set_state(Order.token)
                await callback.message.edit_text(f"Недействительный токен!")
        elif callback.data == "token_edit":
            await state.update_data(token=None)
            file = InputMediaPhoto(media=await db.GetMedia("InstructionPhoto"), 
            caption="""<b>Процесс пошел.</b>
            <blockquote>Первый этап.</blockquote>
            <blockquote>Необходимо создать бота в @BotFather\nНазначить ему имя, ссылку(обязательно)\n<i>Фото, описание(не обязательно)</i></blockquote>
            <blockquote><b>Инструкция.</b>\n/start запускаем @BotFather\n/newbot команда для создания нового бота\nУказываем имя бота\nУказываем ссылку для бота(<i>имя</i>_bot)\nКопируем token и пересылаем в <b>BusinessPoint</b>\n\nДополнительно по команде /mybots можно настроить:\nФото для бота\nОписание бота</blockquote>
            <blockquote>Ждем ваш токен. Он выглядит так:\n<code>числа:буквы</code></blockquote>""",
            parse_mode=ParseMode.HTML)
            await callback.message.edit_media(media=file)
            await state.set_state(Order.token)
    except Exception as e:
        await state.clear()
        #await ExceptionMessageCallback(callback, e)

@router.message(F.photo)
async def GetImageId(message: Message):
    print(message.photo[-1].file_id)

@router.message(F.video)
async def GetVideoId(message: Message):
    print(message.video.file_id)