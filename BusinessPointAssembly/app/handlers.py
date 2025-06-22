from aiogram import F, Router, Bot
from aiogram.filters import CommandStart
from aiogram.types import Message, CallbackQuery, InputMediaPhoto, InputMediaVideo, LabeledPrice, PreCheckoutQuery, SuccessfulPayment
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
        await message.answer_photo(
            caption=f"""<b>Привет, это BusinessPoint.\nТеперь ты зарегестрирован!</b>
            <blockquote>Универсальный инструмент для создания адаптивных CRM систем для владельцев малого бизнеса в Telegram!</blockquote>""",
            parse_mode=ParseMode.HTML,
            photo=await db.GetMedia("MainPhoto"),
            reply_markup=await kb.StartMenu())
        return
    else:
        await message.answer_photo(
            caption=f"""<b>Привет {message.from_user.full_name}, это BusinessPoint.</b>
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
        caption=f"""<b>Меню конфигурации.</b>
        <blockquote>Имя бота: @{bot.Name}\nТокен: <code>{bot.Token}</code></blockquote>
        <blockquote>Тут можно конфигурировать вашего бота.</blockquote>""",
        parse_mode=ParseMode.HTML)
    await callback.message.edit_media(media=file,
                                        reply_markup=await kb.ConfigureBot(callback.data[4:]))
    
@router.callback_query(F.data.startswith('delete_'))
async def DeleteBot(callback: CallbackQuery):
    await db.DeleteBot(callback.from_user.id, callback.data[7:])
    await callback.bot.answer_callback_query(text=f'Бот был удалён', 
                                                callback_query_id=callback.id, show_alert=True)
    await callback.message.delete()

@router.callback_query(F.data == 'profile')
async def ShowProfile(callback: CallbackQuery):
    isVip = await db.IsUserVip(callback.from_user.id)
    if isVip:
        sub = "Активна⭐️"
    else:
        sub = "Не активна (ограничение на 3-х ботов)"
    file = InputMediaPhoto(media=await db.GetMedia("MainPhoto"), 
        caption=f"""<b>Ваш профиль.</b>
        <blockquote>Имя: {callback.from_user.full_name}</blockquote>
        <blockquote>Подписка: {sub}</blockquote>""",
        parse_mode=ParseMode.HTML)
    await callback.message.edit_media(media=file,
                                        reply_markup=await kb.SubscribeMenu())

@router.callback_query(F.data == 'paysub')
async def pay_subscription(callback: CallbackQuery, bot: Bot):
    await callback.message.delete()  # или отредактируй без кнопок

    await bot.send_invoice(
        chat_id=callback.from_user.id,
        title="Business подписка",
        description="Разблокирует неограниченное количество ботов",
        provider_token="STARS",  # Специальное значение для оплаты звёздами
        currency="XTR",
        prices=[LabeledPrice(label="Подписка", amount=1000)],
        payload="stars_payment"
    )

    await callback.answer()

@router.pre_checkout_query()
async def checkout(pre_checkout_query: PreCheckoutQuery):
    await pre_checkout_query.answer(ok=True)

@router.message(F.text == "/fakepay")
async def successful_payment(message: Message):
    await message.answer(f"Business подписка оплачена!")
    await db.SetVip(message.from_user.id)
    await Start(message)

@router.message(F.photo)
async def GetImageId(message: Message):
    print(message.photo[-1].file_id)

@router.message(F.video)
async def GetVideoId(message: Message):
    print(message.video.file_id)