from aiogram import F, Router
from aiogram.filters import CommandStart
from aiogram.types import Message, CallbackQuery, InputMediaPhoto, InputMediaVideo
from aiogram.fsm.state import StatesGroup, State
from aiogram.fsm.context import FSMContext

import database.db_utils as db
import app.keyboards as kb
import traceback
import re

router = Router()

class Order(StatesGroup):
    music = State()
    nickname = State()
    wishes = State()
    confirmation = State()

def contains_emoji(text):
    return bool(re.search(r'[^\w\s,]', text))

#region ---Closing Handlers---
@router.callback_query(F.data == 'gameback')
async def GetBack(callback: CallbackQuery):
    file = InputMediaVideo(media=await db.GetMedia("MainVideo"), 
                           caption="Привет, мы студия IBUYMOVIE - команда профессиональных эдиторов и дизайнеров, готовых выполнить работу любой сложности.")
    await callback.message.edit_media(media=file, 
                                      reply_markup=await kb.StartMenu())

@router.callback_query(F.data == 'roundsback')
async def GetBack(callback: CallbackQuery):
    await GameList(callback)

@router.callback_query(F.data == 'closeInstruction')
async def CloseInstuction(callback: CallbackQuery):
    file = InputMediaVideo(media=await db.GetMedia("MainVideo"), 
                           caption="Привет, мы студия IBUYMOVIE - команда профессиональных эдиторов и дизайнеров, готовых выполнить работу любой сложности.")
    await callback.message.edit_media(media=file, 
                                      reply_markup=await kb.StartMenu())

@router.callback_query(F.data == 'closePreview')
async def CloseInstuction(callback: CallbackQuery):
    await GameList(callback)
    
@router.callback_query(F.data.startswith('cancelOrder'))
async def CancelOrder(callback: CallbackQuery):
    await db.UpdateFillingStatus(callback.from_user.id)
    await db.DeleteOrder(callback.from_user.id)
    await callback.bot.answer_callback_query(text='Заказ отменён', 
                                            callback_query_id=callback.id, show_alert=True)
    await GameList(callback)
#endregion

#region ---Menu Handlers---
@router.message(CommandStart())
async def Start(message: Message):
    user = await db.IsUser(message.chat.id)
    if user == None:
        await db.AddUser(message.chat.id, message.chat.username)
        print(message.chat.username)
    if user:
        print('User exists and banned')
    else:
        await message.answer_video(caption="Привет, мы студия IBUYMOVIE - команда профессиональных эдиторов и дизайнеров, готовых выполнить работу любой сложности.",
                                   video=await db.GetMedia("MainVideo"),
                                   reply_markup=await kb.StartMenu())

@router.callback_query(F.data == 'start')
async def GameList(callback: CallbackQuery):
    banned = await db.IsUser(callback.from_user.id)
    if not banned:
        file = InputMediaPhoto(media=await db.GetMedia("OrderPhoto"), caption="Выберите игру:")
        await callback.message.edit_media(media=file,
                                          reply_markup=await kb.BuildGames(await db.GetGames()))

@router.callback_query(F.data.startswith('gameinstruction'))
async def SendInstruction(callback: CallbackQuery):
    if callback.data == 'gameinstruction':
        file = InputMediaVideo(media=await db.GetMedia("OrderInstructionVideo"), 
                            caption="Инструкция по заказу в IBUYMOVIE")
        await callback.message.edit_media(media=file, 
                                        reply_markup=kb.closeSpecial)
    else:
        file = InputMediaVideo(media=await db.GetMedia("OrderInstructionVideo"), 
                            caption="Инструкция по заказу в IBUYMOVIE")
        await callback.message.edit_media(media=file, 
                                        reply_markup=kb.closeInstruction)

@router.callback_query(F.data.startswith('game'))
async def GameInfo(callback: CallbackQuery):
    if callback.message.video:
        file = InputMediaPhoto(media=await db.GetMedia("OrderPhoto"), caption="Выберите игру:")
        await callback.message.edit_media(media=file,
                                          reply_markup=await kb.BuildGames(await db.GetGames()))
    else:
        if callback.data != 'gamespecial':
            if await db.IsRoundBased(callback.data):
                await callback.message.edit_caption(
                    caption=f'Игра: {await db.GetGameNameByCallbackData(callback.data)}\nЗаполни заявку по примеру:',
                    reply_markup=await kb.BuildRounds(callback.data))
            else:
                await callback.message.edit_caption(
                    caption=f'Игра: {await db.GetGameNameByCallbackData(callback.data)}\nЗаполни заявку по примеру:',
                    reply_markup=await kb.BuildDuration(callback.data))
        else:
            await callback.message.edit_caption(
                    caption=f'Спец. заявки обсуждаются в лс @jaizer1337', reply_markup=kb.closeSpecial)

@router.callback_query(F.data.startswith('place') | F.data.startswith('prevord'))
async def OrderInfo(callback: CallbackQuery):
    if callback.data.startswith("prevord"):
        # Game.GameId, Game.Name, Game.CallbackData, Game.IsRoundBased, Preview.ShortDescription, Preview.VideoId, Preview.PreviewId
        game = await db.GetGameByPreviewId(callback.data.replace('prevord', ''))
        if game[0][3] == True:
            file = InputMediaVideo(media=game[0][5], caption=f'Выбери количество раундов для оформления заказа\n\nИгра: {game[0][1]}\nЗаявка по примеру: {game[0][4]}')
            await callback.message.edit_media(media=file,
                                                reply_markup=await kb.BuildRoundsOrderPreview(game[0][2], game[0][6]))
        else:
            file = InputMediaVideo(media=game[0][5], caption=f'Выбери длительность для оформления заказа\n\nИгра: {game[0][1]}\nЗаявка по примеру: {game[0][4]}')
            await callback.message.edit_media(media=file,
                                                reply_markup=await kb.BuildDurationOrderPreview(game[0][2], game[0][6]))
    else:
        if callback.data.startswith("placeorderRound"):
            callbackData = callback.data.replace("placeorderRound", '')
            await callback.message.edit_caption(
                caption=f'Игра: {await db.GetGameNameByCallbackData(callbackData)}\nЗаполни заявку по примеру:',
                reply_markup=await kb.BuildRoundsOrder(callbackData))
            
        elif callback.data.startswith("placeorderDuration"):
            callbackData = callback.data.replace("placeorderDuration", '')
            await callback.message.edit_caption(
                caption=f'Игра: {await db.GetGameNameByCallbackData(callbackData)}\nЗаполни заявку по примеру:',
                reply_markup=await kb.BuildDurationOrder(callbackData))
        else:
            await GameInfo(callback.data.replace("place", ''))


@router.callback_query(F.data.startswith('preview'))
async def PreviewList(callback: CallbackQuery):
    file = InputMediaPhoto(media=await db.GetMedia("OrderPhoto"), caption=f'Пример по игре: {await db.GetGameNameByCallbackData(callback.data)}')
    await callback.message.edit_media(media=file,
                                        reply_markup=await kb.BuildPreview(await db.GetPreviewByCallbackData(callback.data)))

@router.callback_query(F.data.startswith('video'))
async def ShowPreview(callback: CallbackQuery):
    result = await db.GetVideoDataIdByPreviewId(int(callback.data.replace('video','')))
    result.LongDescription = result.LongDescription.replace(". ", ".\n\n")
    file = InputMediaVideo(media=result.VideoId, 
                           caption=result.LongDescription)
    await callback.message.edit_media(media=file, 
                                      reply_markup=await kb.VideoPreview(callback.data))
#endregion

#region ---Order Handlers---
async def StartOrder(callback: CallbackQuery, state: FSMContext):
    if callback.data.startswith("round") or callback.data.startswith("time"):
        print(callback.data)
        game = re.sub(r'.*?(?=game)', '', callback.data)
        print(game)
        duration = callback.data.replace('round', '').replace('time', '').replace(game, '')
        durationDict = {1: "1 раунд", 2: "2 раунда", 3: "3+ раунда", 4: "Другая длительность(раунды)",
                        5: "30 секунд", 6: "1 минута", 7: "Больше 1 минуты", 8: "Другая длительность(минуты)"}
        await db.UpdateFillingStatus(callback.from_user.id)
        await db.AddOrder(callback.from_user.id, await db.GetGameIdByCallbackData(game), durationDict[int(duration)])
        file = InputMediaPhoto(media=await db.GetMedia("OrderPhoto"), caption=f'Заполнение заказа:\nИгра: {await db.GetGameNameByCallbackData(game)}\nДлительность: {durationDict[int(duration)]}\n\nУкажи название/ссылку на трек')
        await callback.message.edit_media(media=file)
        await state.set_state(Order.music)
    else:
        print(callback.data)
        match = re.search(r'(?:pround|ptime)(\d+).*?game(.*)-(\d+)', callback.data)
        if match:
            duration = match.group(1)
            game = f"game{match.group(2)}"
            PreviewId = match.group(3)
        print(game)
        print(PreviewId)
        durationDict = {1: "1 раунд", 2: "2 раунда", 3: "3+ раунда", 4: "Другая длительность(раунды)",
                        5: "30 секунд", 6: "1 минута", 7: "Больше 1 минуты", 8: "Другая длительность(минуты)"}
        await db.UpdateFillingStatus(callback.from_user.id)
        await db.AddOrder(callback.from_user.id, await db.GetGameIdByCallbackData(game), durationDict[int(duration)])
        await db.UpdateOrder(await db.GetOrderIdByUserId(callback.from_user.id), "PreviewId", PreviewId)
        file = InputMediaPhoto(media=await db.GetMedia("OrderPhoto"), caption=f'Заполнение заказа:\nИгра: {await db.GetGameNameByCallbackData(game)}\nДлительность: {durationDict[int(duration)]}\n\nУкажи название/ссылку на трек')
        await callback.message.edit_media(media=file)
        await state.set_state(Order.music)

async def ExceptionMessageCallback(callback: CallbackQuery, exception: Exception):
    await callback.message.delete()
    await db.DeleteOrder(callback.from_user.id)
    await db.UpdateFillingStatus(callback.from_user.id)
    await db.AddError(callback.from_user.id, str(exception))
    banned = await db.IsUser(callback.from_user.id)
    if not banned:
        #tb = exception.__traceback__
        #tb_info = traceback.extract_tb(tb)[-1]
        #filename, lineno, function = tb_info
        #print(f"Исключение произошло в файле {filename}, строка {lineno}, функция {function}")
        await callback.message.answer("Произошла ошибка при заполнении заказа\nЕсли она повторится, обратитесь к @jaizer1337")
        await callback.bot.send_message(chat_id=344249045, 
                text=f"Ошибка была вызвана юзером: {callback.from_user.id} - @{callback.from_user.username}\n\nОшибка:\n{exception}")
        await Start(callback.message)
    else:
        await callback.bot.send_message(chat_id=344249045, 
                    text=f"Ошибка была вызвана ЗАБАННЕНЫМ юзером: {callback.from_user.id} - @{callback.from_user.username}\n\nОшибка:\n{exception}")

async def ExceptionMessage(message: Message, exception: Exception):
    await message.delete()
    await db.DeleteOrder(message.from_user.id)
    await db.UpdateFillingStatus(message.from_user.id)
    await db.AddError(message.from_user.id, str(exception))
    banned = await db.IsUser(message.from_user.id)
    if not banned:
        await message.answer("Произошла ошибка при заполнении заказа\nЕсли она повторится, обратитесь к @jaizer1337")
        await message.bot.send_message(chat_id=344249045, 
                text=f"Ошибка была вызвана юзером: {message.from_user.id} - @{message.from_user.username}\n\nОшибка:\n{exception}")
        await Start(message)
    else:
        await message.bot.send_message(chat_id=344249045, 
                text=f"Ошибка была вызвана ЗАБАННЕНЫМ юзером: {message.from_user.id} - @{message.from_user.username}\n\nОшибка:\n{exception}")

@router.callback_query(F.data.startswith('round') | F.data.startswith('pround'))
async def RoundOrderStart(callback: CallbackQuery, state: FSMContext):
    if await db.GetFillingStatus(callback.from_user.id) == False:
        await StartOrder(callback, state)
    else:
        await callback.bot.answer_callback_query(text='Вы уже заполняете заказ', 
                                                 callback_query_id=callback.id, show_alert=True)

@router.callback_query(F.data.startswith('time') | F.data.startswith('ptime'))
async def TimeOrderStart(callback: CallbackQuery, state: FSMContext):
    if await db.GetFillingStatus(callback.from_user.id) == False:
        await StartOrder(callback, state)
    else:
        await callback.bot.answer_callback_query(text='Вы уже заполняете заказ', 
                                                 callback_query_id=callback.id, show_alert=True)

# Этап 1: Получение музыки
@router.message(F.content_type.in_({'text'}), Order.music)
async def GetMusic(message: Message, state: FSMContext):
    try:
        data = await state.get_data()
        if not data.get("music"):
            await state.update_data(music=message.text)
            await message.answer(
                text=f"Музыка: {message.text[:200]}",
                reply_markup=await kb.ConfirmButtons("music")
            )
    except Exception as e:
        await state.clear()
        await ExceptionMessage(message, e)

@router.callback_query(lambda c: c.data in ["music_confirm", "music_edit"])
async def ConfirmMusic(callback: CallbackQuery, state: FSMContext):
    try:
        if callback.data == "music_confirm":
            data = await state.get_data()
            await db.UpdateOrder(await db.GetOrderIdByUserId(callback.from_user.id), "Music", data["music"][:200])
            await callback.message.edit_text(f"Музыка: {data["music"][:200]}\n\nУкажи свой никнейм:")
            await state.set_state(Order.nickname)
        elif callback.data == "music_edit":
            await state.update_data(music=None)  # Убираем старое значение музыки
            await callback.message.edit_text("Укажи название/ссылку на трек:")
            await state.set_state(Order.music)
    except Exception as e:
        await state.clear()
        await ExceptionMessageCallback(callback, e)

# Этап 2: Получение никнейма
@router.message(F.content_type.in_({'text'}), Order.nickname)
async def GetNickname(message: Message, state: FSMContext):
    try:
        data = await state.get_data()
        if not data.get("nickname"):
            await state.update_data(nickname=message.text)
            await message.answer(
                text=f"Никнейм: {message.text[:50]}",
                reply_markup=await kb.ConfirmButtons("nickname")
            )
    except Exception as e:
        await state.clear()
        await ExceptionMessage(message, e)

@router.callback_query(lambda c: c.data in ["nickname_confirm", "nickname_edit"])
async def ConfirmNickname(callback: CallbackQuery, state: FSMContext):
    try:
        if callback.data == "nickname_confirm":
            data = await state.get_data()
            await db.UpdateOrder(await db.GetOrderIdByUserId(callback.from_user.id), "Nickname", data["nickname"][:50])
            await callback.message.edit_text(f"Музыка: {data["music"][:200]}\nНикнейм: {data["nickname"][:50]}\n\nУкажи пожелания к мувику:")
            await state.set_state(Order.wishes)
        elif callback.data == "nickname_edit":
            await state.update_data(nickname=None)  # Убираем старое значение никнейма
            data = await state.get_data()
            await callback.message.edit_text(f"Музыка: {data["music"][:200]}\n\nУкажи свой никнейм:")
            await state.set_state(Order.nickname)
    except Exception as e:
        await state.clear()
        await ExceptionMessageCallback(callback, e)

# Этап 3: Получение пожеланий
@router.message(F.content_type.in_({'text'}), Order.wishes)
async def GetWishes(message: Message, state: FSMContext):
    try:
        data = await state.get_data()
        if not data.get("wishes"):
            await state.update_data(wishes=message.text)
            await message.answer(
                text=f"Пожелания к мувику: {message.text[:700]}",
                reply_markup=await kb.ConfirmButtons("wishes")
            )
    except Exception as e:
        await state.clear()
        await ExceptionMessage(message, e)

@router.callback_query(lambda c: c.data in ["wishes_confirm", "wishes_edit"])
async def ConfirmWishes(callback: CallbackQuery, state: FSMContext):
    try:
        if callback.data == "wishes_confirm":
            await state.set_state(Order.confirmation)  # Переходим к финальному этапу
            data = await state.get_data()
            await db.UpdateOrder(await db.GetOrderIdByUserId(callback.from_user.id), "Wishes", data["wishes"][:600])
            previewVideo = await db.GetOrderPreviewByUserId(callback.from_user.id)
            if previewVideo != None:
                confirmation_text = (
                    f"Ваш заказ:\n"
                    f"Игра: {await db.GetGameNameByUserId(callback.from_user.id)}\n"
                    f"Длительность: {await db.GetOrderDurationByUserId(callback.from_user.id)}\n\n"
                    f"Музыка: {await db.GetOrderMusicByUserId(callback.from_user.id)}\n\n"
                    f"Никнейм: {await db.GetOrderNicknameByUserId(callback.from_user.id)}\n\n"
                    f"Пожелания: {await db.GetOrderWishesByUserId(callback.from_user.id)}\n\n"
                )
                file = InputMediaVideo(media=previewVideo, caption=confirmation_text)
                await callback.message.edit_media(media=file,
                                                    reply_markup=await kb.FinalButtons())
            else:
                confirmation_text = (
                    f"Ваш заказ:\n"
                    f"Игра: {await db.GetGameNameByUserId(callback.from_user.id)}\n"
                    f"Длительность: {await db.GetOrderDurationByUserId(callback.from_user.id)}\n\n"
                    f"Музыка: {await db.GetOrderMusicByUserId(callback.from_user.id)}\n\n"
                    f"Никнейм: {await db.GetOrderNicknameByUserId(callback.from_user.id)}\n\n"
                    f"Пожелания: {await db.GetOrderWishesByUserId(callback.from_user.id)}\n\n"
                )
                file = InputMediaPhoto(media=await db.GetMedia("OrderPhoto"), caption=confirmation_text)
                await callback.message.edit_media(media=file,
                                                    reply_markup=await kb.FinalButtons())
        elif callback.data == "wishes_edit":
            await state.update_data(wishes=None)  # Убираем старое значение пожеланий
            data = await state.get_data()
            await callback.message.edit_text(f"Музыка: {data["music"][:200]}\nНикнейм:{data["nickname"][:50]}\n\nУкажи пожелания к мувику:")
            await state.set_state(Order.wishes)
    except Exception as e:
        await state.clear()
        await ExceptionMessageCallback(callback, e)

# Финальное подтверждение или отмена заказа
@router.callback_query(lambda c: c.data in ["order_confirm", "order_cancel"])
async def FinalConfirmation(callback: CallbackQuery, state: FSMContext):
    try:
        data = await state.get_data()
        if callback.data == "order_confirm":
            previewVideo = await db.GetOrderPreviewByUserId(callback.from_user.id)
            if previewVideo != None:
                order_summary = (
                    f"Новый заказ:\n\n"
                    f"Пользователь: @{callback.from_user.username} - Id: {callback.from_user.id}\n\n"
                    f"Игра: {await db.GetGameNameByUserId(callback.from_user.id)}\n\n"
                    f"Длительность: {await db.GetOrderDurationByUserId(callback.from_user.id)}\n\n"
                    f"Музыка: {await db.GetOrderMusicByUserId(callback.from_user.id)}\n\n"
                    f"Никнейм: {await db.GetOrderNicknameByUserId(callback.from_user.id)}\n\n"
                    f"Пожелания: {await db.GetOrderWishesByUserId(callback.from_user.id)}"
                )
            else:
                order_summary = (
                    f"Новый заказ:\n\n"
                    f"Пользователь: @{callback.from_user.username} - Id: {callback.from_user.id}\n\n"
                    f"Игра: {await db.GetGameNameByUserId(callback.from_user.id)}\n\n"
                    f"Длительность: {await db.GetOrderDurationByUserId(callback.from_user.id)}\n\n"
                    f"Музыка: {await db.GetOrderMusicByUserId(callback.from_user.id)}\n\n"
                    f"Никнейм: {await db.GetOrderNicknameByUserId(callback.from_user.id)}\n\n"
                    f"Пожелания: {await db.GetOrderWishesByUserId(callback.from_user.id)}"
                )
            banned = await db.IsUser(callback.from_user.id)
            if not banned:
                if previewVideo != None:
                    await callback.bot.send_video(video=previewVideo, chat_id=344249045, caption=order_summary, reply_markup=await kb.SendNotification(callback.from_user.id))
                else:
                    await callback.bot.send_message(chat_id=344249045, text=order_summary, reply_markup=await kb.SendNotification(callback.from_user.id))
                await db.CompleteOrder(await db.GetOrderIdByUserId(callback.from_user.id))
                await db.UpdateFillingStatus(callback.from_user.id)
                await GameList(callback)
                await callback.bot.answer_callback_query(text="Заказ подтвержден и отправлен!", 
                                    callback_query_id=callback.id, show_alert=True)
                await state.clear()
            else:
                await callback.message.delete()
                await callback.message.answer(text="Ты был ЗАБАНЕН в IBUYMOVIE")
                await callback.message.answer_sticker(await db.GetMedia("BannedSticker"))
                await db.DeleteOrder(callback.from_user.id) # Удаляем заказ из БД
                await state.clear()
        elif callback.data == "order_cancel":
            banned = await db.IsUser(callback.from_user.id)
            if not banned:
                await db.DeleteOrder(callback.from_user.id) # Удаляем заказ из БД
                await CancelOrder(callback)
                await state.clear()
            else:
                await callback.message.delete()
                await callback.message.answer(text="Ты был ЗАБАНЕН в IBUYMOVIE")
                await callback.message.answer_sticker(await db.GetMedia("BannedSticker"))
                await db.DeleteOrder(callback.from_user.id) # Удаляем заказ из БД
                await state.clear()
    except Exception as e:
        await state.clear()
        await ExceptionMessageCallback(callback, e)

@router.callback_query(F.data.startswith('note'))
async def SendNotification(callback: CallbackQuery):
    await callback.bot.send_message(chat_id=callback.data.replace('note',''), text="Откройте лс для @jaizer1337, для связи по поводу заказа")
    await callback.bot.answer_callback_query(text='Уведомление отправлено', 
                                                 callback_query_id=callback.id, show_alert=True)
#endregion

#region ---Misc Handlers---
@router.message(F.photo)
async def GetImageId(message: Message):
    print(message.photo[-1].file_id)

@router.message(F.video)
async def GetVideoId(message: Message):
    print(message.video.file_id)
#endregion