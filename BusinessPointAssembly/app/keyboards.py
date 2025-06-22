from aiogram import Bot
from aiogram.types import InlineKeyboardMarkup, InlineKeyboardButton
from aiogram.utils.keyboard import InlineKeyboardBuilder

import database.db_utils as db

async def StartMenu():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Список ботов", callback_data='list_bot'))
    keyboard.add(InlineKeyboardButton(text="Профиль", callback_data='profile'))
    return keyboard.adjust(1).as_markup()

async def SubscribeMenu():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Оплатить подписку", callback_data='paysub'))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data='botback'))
    return keyboard.adjust(1).as_markup()

async def PayingMenu():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Оплатить 1000⭐️", pay=True))
    return keyboard.adjust(1).as_markup()

async def BotCreateList():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="FAQ Бот", callback_data='create_FAQ'))
    return keyboard.adjust(1).as_markup()

async def ConfirmHash():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Подтвердить токен", callback_data=f"token_confirm"))
    keyboard.add(InlineKeyboardButton(text="Изменить", callback_data=f"token_edit"))
    keyboard.add(InlineKeyboardButton(text="Отменить настройку бота", callback_data=f"create_cancel"))
    return keyboard.adjust(1).as_markup()


async def ConfigureBot(Token: str):
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Удалить бота", callback_data=f"delete_{Token}"))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data='botback'))
    return keyboard.adjust(1).as_markup()

async def BotList():
    keyboard = InlineKeyboardBuilder()
    keyboard.add(InlineKeyboardButton(text="Список ботов", callback_data='list_bot'))
    return keyboard.adjust(1).as_markup()

async def BuildBots(bots: list):
    keyboard = InlineKeyboardBuilder()
    for bot in bots:
        keyboard.add(InlineKeyboardButton(text=bot.Name, callback_data=f"bot_{bot.Token}"))
    keyboard.add(InlineKeyboardButton(text='Назад', callback_data='botback'))
    return keyboard.adjust(1).as_markup()