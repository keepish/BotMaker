from aiogram.types import InlineKeyboardMarkup, InlineKeyboardButton
from aiogram.utils.keyboard import InlineKeyboardBuilder

import database.db_utils as db

#region ---Single Buttons---
closeInstruction = InlineKeyboardMarkup(inline_keyboard=[
    [InlineKeyboardButton(text="Закрыть", callback_data='closeInstruction')]
])

closeSpecial = InlineKeyboardMarkup(inline_keyboard=[
    [InlineKeyboardButton(text="Назад", callback_data='start')]
])

async def StartMenu():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Инструкция по оформлению заказа", callback_data='gameinstructionstart'))
    keyboard.add(InlineKeyboardButton(text="Оформить заказ", callback_data='start'))
    keyboard.add(InlineKeyboardButton(text="Больше примеров", url="https://vkvideo.ru/@ibuymovie/playlists"))
    return keyboard.adjust(1).as_markup()

async def SendNotification(UserId: int):
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Попросить открыть лс", callback_data=f"note{UserId}"))
    return keyboard.adjust(1).as_markup()

async def ClosePreview(PreviewId: str) -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(inline_keyboard=[
        [InlineKeyboardButton(text="Закрыть", callback_data=f'preview{await db.GetCallbackDataByPreviewId(PreviewId)}')]
    ])

async def VideoPreview(PreviewId: str) -> InlineKeyboardMarkup:
    keyboard = InlineKeyboardBuilder()
    print(f"prevord{PreviewId.replace('video', '')}")
    keyboard.add(InlineKeyboardButton(text='Оформить заказ по этому примеру', callback_data=f'prevord{PreviewId.replace('video', '')}'))
    keyboard.add(InlineKeyboardButton(text="Закрыть", callback_data=f'preview{await db.GetCallbackDataByPreviewId(PreviewId)}'))
    return keyboard.adjust(1).as_markup()
#endregion

#region ---Misc Buttons---
async def ConfirmButtons(CallbackData):
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Подтвердить", callback_data=f"{CallbackData}_confirm"))
    keyboard.add(InlineKeyboardButton(text="Изменить", callback_data=f"{CallbackData}_edit"))
    keyboard.add(InlineKeyboardButton(text="Отменить заказ", callback_data=f"order_cancel"))
    return keyboard.adjust(1).as_markup()

async def FinalButtons():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Подтвердить заказ", callback_data="order_confirm"))
    keyboard.add(InlineKeyboardButton(text="Отменить заказ", callback_data="order_cancel"))
    return keyboard.adjust(1).as_markup()
#endregion

#region ---Builded Buttons---
async def BuildGames(games: list):
    keyboard = InlineKeyboardBuilder()
    for game in games:
        keyboard.add(InlineKeyboardButton(text=game.Name, callback_data=game.CallbackData))
    keyboard.add(InlineKeyboardButton(text='Инструкция по оформлению заказа', callback_data='gameinstruction'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data='gameback'))
    return keyboard.adjust(*await db.GetKeyboardMarkup("BuildGames")).as_markup()

async def BuildPreview(previews: list):
    keyboard = InlineKeyboardBuilder()
    for preview in previews:
        keyboard.add(InlineKeyboardButton(text=preview.ShortDescription, callback_data=f'video{preview.PreviewId}'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data=preview.CallbackData))
    return keyboard.adjust(1).as_markup()

async def BuildRounds(CallbackData: str):
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text=f'Примеры по {await db.GetGameNameByCallbackData(CallbackData)}', 
                                    callback_data=f'preview{CallbackData}'))
    keyboard.add(InlineKeyboardButton(text='Оформить заказ', callback_data=f'placeorderRound{CallbackData}'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data='roundsback'))
    return keyboard.adjust(1).as_markup()

async def BuildDuration(CallbackData: str):
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text=f'Примеры по {await db.GetGameNameByCallbackData(CallbackData)}', 
                                      callback_data=f'preview{CallbackData}'))
    keyboard.add(InlineKeyboardButton(text='Оформить заказ', callback_data=f'placeorderDuration{CallbackData}'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data='roundsback'))
    return keyboard.adjust(1).as_markup()

async def BuildRoundsOrder(CallbackData: str):
    roundNames = ["1 раунд", "2 раунда", "3+ раунда"]
    keyboard = InlineKeyboardBuilder()
    for i in range(len(roundNames)):
        keyboard.add(InlineKeyboardButton(text=roundNames[i], callback_data=f'round{i+1}{CallbackData}'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data=CallbackData))
    return keyboard.adjust(*await db.GetKeyboardMarkup("BuildRounds")).as_markup()

async def BuildDurationOrder(CallbackData: str):
    roundNames = ["30 секунд", "1 минута", "Больше 1 минуты"]
    keyboard = InlineKeyboardBuilder()
    for i in range(len(roundNames)):
        keyboard.add(InlineKeyboardButton(text=roundNames[i], callback_data=f'time{i+5}{CallbackData}'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data=CallbackData))
    return keyboard.adjust(*await db.GetKeyboardMarkup("BuildDuration")).as_markup()

async def BuildRoundsOrderPreview(CallbackData: str, PreviewId: int): 
    roundNames = ["1 раунд", "2 раунда", "3+ раунда"]
    keyboard = InlineKeyboardBuilder()
    for i in range(len(roundNames)):
        keyboard.add(InlineKeyboardButton(text=roundNames[i], callback_data=f'pround{i+1}{CallbackData}-{PreviewId}'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data=f"preview{CallbackData}"))
    return keyboard.adjust(*await db.GetKeyboardMarkup("BuildRounds")).as_markup()

async def BuildDurationOrderPreview(CallbackData: str, PreviewId: int):
    print("dprev")
    roundNames = ["30 секунд", "1 минута", "Больше 1 минуты"]
    keyboard = InlineKeyboardBuilder()
    for i in range(len(roundNames)):
        keyboard.add(InlineKeyboardButton(text=roundNames[i], callback_data=f'ptime{i+5}{CallbackData}-{PreviewId}'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data=f"preview{CallbackData}"))
    return keyboard.adjust(*await db.GetKeyboardMarkup("BuildDuration")).as_markup()
#endregion