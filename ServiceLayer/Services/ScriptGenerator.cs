using System.Text;

namespace BotMaker.ServiceLayer.Services
{
    public class ScriptGenerator
    {
        public string GetScript(string? token, long userId, string companyName, bool addFAQ, bool clientsCanMakeOrders, bool botTracksExpenses, bool trackOrdersInTable, bool addSearchFilter, bool addNotifications)
        {
            var sb = new StringBuilder();

            // Начало скрипта
            sb.AppendLine(@"import asyncio
import logging
import sqlite3
import random
from datetime import datetime, timedelta
");

            if (trackOrdersInTable)
            {
                sb.AppendLine("import tabulate");
            }

            sb.AppendLine($@"
from aiogram import Bot, Dispatcher, types, F
from aiogram.filters import Command
from aiogram.types import ReplyKeyboardMarkup, KeyboardButton, InlineKeyboardMarkup, InlineKeyboardButton
from aiogram.fsm.context import FSMContext
from aiogram.fsm.state import StatesGroup, State
from aiogram.fsm.storage.memory import MemoryStorage
from aiogram.enums import ParseMode
from typing import Union

API_TOKEN = ""{token}""").AppendLine();
            sb.Append("ADMIN_ID = ").AppendLine($"{userId}").AppendLine();

            sb.AppendLine(@"
logging.basicConfig(level=logging.INFO)

bot = Bot(token=API_TOKEN)
dp = Dispatcher(storage=MemoryStorage())

# --- База данных ---
conn = sqlite3.connect(""bot_db.sqlite3"")
cursor = conn.cursor()

# --- Вспомогательные функции ---
def is_admin(user_id: int) -> bool:
    return user_id == ADMIN_ID

def init_db():");

            // Генерация SQL для создания таблиц
            sb.AppendLine("    tables = [");

            if (addFAQ)
            {
                sb.AppendLine(@"        """"""
        CREATE TABLE IF NOT EXISTS faq (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            question TEXT NOT NULL,
            answer TEXT NOT NULL
        )
        """""",");
            }

            if (clientsCanMakeOrders)
            {
                sb.AppendLine(@"        """"""
        CREATE TABLE IF NOT EXISTS products (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            description TEXT,
            price REAL NOT NULL
        )
        """""",");

                sb.AppendLine(@"        """"""
        CREATE TABLE IF NOT EXISTS orders (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            product_id INTEGER NOT NULL,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            delivery_date TEXT NOT NULL,
            status TEXT DEFAULT 'new',
            amount REAL NOT NULL DEFAULT 1,
            address TEXT,
            phone TEXT,
            FOREIGN KEY(product_id) REFERENCES products(id)
        )
        """""",");
            }

            sb.AppendLine(@"        """"""
        CREATE TABLE IF NOT EXISTS messages (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            message TEXT NOT NULL,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
        """""",
        """"""
        CREATE TABLE IF NOT EXISTS users (
            user_id INTEGER PRIMARY KEY,
            is_admin INTEGER DEFAULT 0
        )
        """"""");

            if (botTracksExpenses)
            {
                sb.AppendLine(@",
        """"""
        CREATE TABLE IF NOT EXISTS expenses (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            amount REAL NOT NULL,
            price REAL NOT NULL,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            month INTEGER GENERATED ALWAYS AS (strftime('%m', created_at)) STORED,
            year INTEGER GENERATED ALWAYS AS (strftime('%Y', created_at)) STORED
        )
        """"""");
            }

            sb.AppendLine(@"    ]");

            sb.AppendLine(@"
    for table in tables:
        cursor.execute(table)
    conn.commit()");

            if (clientsCanMakeOrders)
            {
                sb.AppendLine(@"
def generate_delivery_date():
    days_to_add = random.randint(2, 10)
    delivery_date = datetime.now() + timedelta(days=days_to_add)
    return delivery_date.strftime(""%Y-%m-%d %H:%M:%S"")");
            }

            // Генерация функций для клавиатур
            if (addFAQ)
            {
                sb.AppendLine(@"
def get_faq_keyboard():
    cursor.execute(""SELECT id, question FROM faq"")
    faqs = cursor.fetchall()
    buttons = [
        [InlineKeyboardButton(text=question, callback_data=f""faq_answer_{faq_id}"")]
        for faq_id, question in faqs
    ]
    return InlineKeyboardMarkup(inline_keyboard=buttons)");
            }

            if (clientsCanMakeOrders)
            {
                sb.AppendLine(@"
def get_products_keyboard():
    cursor.execute(""SELECT id, name, price FROM products"")
    products = cursor.fetchall()
    buttons = [
        [InlineKeyboardButton(text=f""{name} - {price}₽"", callback_data=f""buy_{prod_id}"")]
        for prod_id, name, price in products
    ]
    return InlineKeyboardMarkup(inline_keyboard=buttons)");

                sb.AppendLine(@"
def get_orders_management_keyboard():");

                if (addSearchFilter)
                {
                    sb.AppendLine(@"    return InlineKeyboardMarkup(inline_keyboard=[
        [
            InlineKeyboardButton(text=""🔄 Все заказы"", callback_data=""orders_all""),
            InlineKeyboardButton(text=""📦 Новые"", callback_data=""orders_status_new"")
        ],
        [
            InlineKeyboardButton(text=""🚚 В доставке"", callback_data=""orders_status_shipped""),
            InlineKeyboardButton(text=""✅ Доставленные"", callback_data=""orders_status_delivered"")
        ],
        [
            InlineKeyboardButton(text=""📅 По дате"", callback_data=""orders_by_date""),
            InlineKeyboardButton(text=""💰 По сумме"", callback_data=""orders_by_price"")
        ],
        [
            InlineKeyboardButton(text=""🔍 Поиск по товару"", callback_data=""orders_by_product""),
            InlineKeyboardButton(text=""👤 По клиенту"", callback_data=""orders_by_user"")
        ]
    ])");
                }
                else
                {
                    sb.AppendLine(@"    return InlineKeyboardMarkup(inline_keyboard=[
        [
            InlineKeyboardButton(text=""🔄 Все заказы"", callback_data=""orders_all""),
            InlineKeyboardButton(text=""📦 Новые"", callback_data=""orders_status_new"")
        ],
        [
            InlineKeyboardButton(text=""🚚 В доставке"", callback_data=""orders_status_shipped""),
            InlineKeyboardButton(text=""✅ Доставленные"", callback_data=""orders_status_delivered"")
        ]
    ])");
                }

                sb.AppendLine(@"
def get_order_actions_keyboard(order_id: int) -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(inline_keyboard=[
        [
            InlineKeyboardButton(
                text=""✅ Подтвердить"",
                callback_data=f""order_confirm_{order_id}""
            ),
            InlineKeyboardButton(
                text=""🚚 Отправить"",
                callback_data=f""order_ship_{order_id}""
            )
        ],
        [
            InlineKeyboardButton(
                text=""❌ Отменить"",
                callback_data=f""order_cancel_{order_id}""
            ),
            InlineKeyboardButton(
                text=""✏️ Редактировать"",
                callback_data=f""order_edit_{order_id}""
            )
        ]
    ])");

                sb.AppendLine(@"
def get_orders_action_keyboard():
    return InlineKeyboardMarkup(inline_keyboard=[
        [
            InlineKeyboardButton(text=""✏️ Редактировать"", callback_data=""order_edit""),
            InlineKeyboardButton(text=""✅ Подтвердить доставку"", callback_data=""order_confirm"")
        ],
        [
            InlineKeyboardButton(text=""🚚 Отправить"", callback_data=""order_ship""),
            InlineKeyboardButton(text=""❌ Отменить"", callback_data=""order_cancel"")
        ],
        [
            InlineKeyboardButton(text=""🔍 Подробнее"", callback_data=""order_details""),");

                if (addSearchFilter)
                {
                    sb.AppendLine(@"            InlineKeyboardButton(text=""📋 Фильтры"", callback_data=""orders_filters"")");
                }

                sb.AppendLine(@"        ]
    ])");

                sb.AppendLine(@"
def format_orders_table(orders):");

                if (trackOrdersInTable)
                {
                    sb.AppendLine(@"
    if not orders:
        return ""Список заказов пуст""

    headers = [""ID заказа"", ""ID пользователя"", ""Товар"", ""Цена"", ""Дата заказа""]
    table_data = [headers]

    for order in orders:
        table_data.append([
            str(order[0]),  # ID заказа
            str(order[1]),  # ID пользователя
            order[2],  # Название товара
            f""{order[3]}₽"",  # Цена
            order[4]  # Дата заказа
        ])

    # Вычисляем ширину колонок
    col_widths = [
        max(len(str(row[i])) for row in table_data)
        for i in range(len(headers))
    ]

    # Формируем строки таблицы
    lines = [
        "" | "".join(str(item).ljust(col_widths[i]) for i, item in enumerate(row))
        for row in table_data
    ]

    return ""```\n"" + ""\n"".join(lines) + ""\n```""");
                }
                else
                {
                    sb.AppendLine(@"
    if not orders:
        return ""Список заказов пуст""

    status_icons = {
        'new': '🆕',
        'processing': '🔄',
        'shipped': '🚚',
        'delivered': '✅',
        'canceled': '❌'
    }

    text = ""Список заказов:\n\n""
    for order in orders:
        order_id, user_id, product_name, price, created_at, status = order
        text += (
            f""🆔 {order_id} | 👤 {user_id}\n""
            f""📦 {product_name} - {price}₽\n""
            f""📅 {created_at} | {status_icons.get(status, '')} {status}\n""
            f""————————————————\n""
        )
    return text");
                }
            }

            // Генерация состояний FSM
            sb.AppendLine(@"
# --- Модели состояний (FSM) ---");

            if (addFAQ)
            {
                sb.AppendLine(@"class FAQStates(StatesGroup):
    waiting_for_question = State()
    waiting_for_answer = State()
    waiting_for_edit_id = State()
    waiting_for_edit_question = State()
    waiting_for_edit_answer = State()
    waiting_for_delete_id = State()");
            }

            if (clientsCanMakeOrders)
            {
                sb.AppendLine(@"class ProductStates(StatesGroup):
    waiting_for_name = State()
    waiting_for_description = State()
    waiting_for_price = State()
    waiting_for_edit_id = State()
    waiting_for_edit_name = State()
    waiting_for_edit_description = State()
    waiting_for_edit_price = State()
    waiting_for_delete_id = State()");
            }

            if (botTracksExpenses)
            {
                sb.AppendLine(@"class ExpenseStates(StatesGroup):
    waiting_for_name = State()
    waiting_for_amount = State()
    waiting_for_price = State()
    waiting_for_month = State()");
            }

            if (clientsCanMakeOrders)
            {
                sb.AppendLine(@"class OrderStates(StatesGroup):");

                if (addSearchFilter)
                {
                    sb.AppendLine(@"    # Состояния для фильтрации заказов
    waiting_for_date_filter = State()
    waiting_for_price_filter = State()
    waiting_for_product_filter = State()
    waiting_for_user_filter = State()");
                }

                sb.AppendLine(@"    # Состояния для редактирования заказа
    waiting_for_edit_order = State()
    waiting_for_edit_field = State()
    waiting_for_edit_value = State()

    # Состояния для оформления заказа
    waiting_for_order_amount = State()
    waiting_for_order_address = State()
    waiting_for_order_phone = State()

    # Состояния для изменения статуса
    waiting_for_status_change = State()
    waiting_for_order_to_change = State()
    wathing_order_by_order_id = State()");
            }

            sb.AppendLine(@"class MessageStates(StatesGroup):
    waiting_for_message = State()");

            // Генерация клавиатур
            sb.AppendLine(@"
# --- Клавиатуры ---
def create_user_keyboard():
    return ReplyKeyboardMarkup(
        keyboard=[");

            var userButtons = new List<string>();
            if (addFAQ) userButtons.Add(@"KeyboardButton(text=""FAQ"")");
            if (clientsCanMakeOrders) userButtons.Add(@"KeyboardButton(text=""Ассортимент"")");
            userButtons.Add(@"KeyboardButton(text=""Мои обращения"")");
            if (clientsCanMakeOrders) userButtons.Add(@"KeyboardButton(text=""Мои заказы"")");

            sb.Append("            [").Append(string.Join(", ", userButtons)).AppendLine("],");
            sb.AppendLine(@"        ],
        resize_keyboard=True
    )");

            sb.AppendLine(@"
def create_admin_keyboard():
    buttons = [");

            var adminButtons = new List<string>();
            if (addFAQ) adminButtons.Add(@"KeyboardButton(text=""Редактировать FAQ"")");
            if (clientsCanMakeOrders) adminButtons.Add(@"KeyboardButton(text=""Редактировать товары"")");
            adminButtons.Add(@"KeyboardButton(text=""Просмотреть обращения"")");
            if (clientsCanMakeOrders) adminButtons.Add(@"KeyboardButton(text=""Просмотреть заказы"")");
            if (botTracksExpenses)
            {
                adminButtons.Add(@"KeyboardButton(text=""Добавить расходы"")");
                adminButtons.Add(@"KeyboardButton(text=""Просмотреть расходы"")");
                adminButtons.Add(@"KeyboardButton(text=""Статистика прибыли"")");
            }

            // Разбиваем кнопки на строки по 2 кнопки
            for (int i = 0; i < adminButtons.Count; i += 2)
            {
                var rowButtons = adminButtons.Skip(i).Take(2);
                sb.Append("        [").Append(string.Join(", ", rowButtons)).AppendLine("],");
            }

            sb.AppendLine(@"    ]
    return ReplyKeyboardMarkup(
        keyboard=buttons,
        resize_keyboard=True
    )");

            // Генерация обработчиков команд
            sb.AppendLine(@"
# --- Обработчики команд ---
@dp.message(Command(""start""))
async def cmd_start(message: types.Message):
    user_id = message.from_user.id
    cursor.execute(""INSERT OR IGNORE INTO users(user_id, is_admin) VALUES (?, ?)"",
               (user_id, int(is_admin(user_id))))
    conn.commit()

    if is_admin(user_id):
        await message.answer(""Привет, админ! Выберите действие:"", reply_markup=create_admin_keyboard())
    else:
        await message.answer(f""""""Вас приветствует компания ").Append(companyName).AppendLine(@"! Выберите действие:"""""",
                             reply_markup=create_user_keyboard())");

            sb.AppendLine(@"
@dp.message(Command(""support""))
async def support_start(message: types.Message, state: FSMContext):
    await message.answer(""Опишите вашу проблему или вопрос:"")
    await state.set_state(MessageStates.waiting_for_message)");

            // Генерация обработчиков FAQ
            if (addFAQ)
            {
                sb.AppendLine(@"
# --- Обработчики FAQ ---
@dp.message(F.text == ""FAQ"")
async def faq_menu(message: types.Message):
    kb = get_faq_keyboard()
    await message.answer(
        ""Выберите вопрос:"" if kb.inline_keyboard else ""FAQ пока пуст."",
        reply_markup=kb
    )

@dp.callback_query(F.data.startswith(""faq_answer_""))
async def faq_answer(callback: types.CallbackQuery):
    try:
        faq_id = int(callback.data.split(""_"")[2])
        cursor.execute(""SELECT answer FROM faq WHERE id = ?"", (faq_id,))
        if row := cursor.fetchone():
            await callback.message.answer(row[0])
        else:
            await callback.message.answer(""Вопрос не найден."")
    except (ValueError, IndexError):
        await callback.answer(""Ошибка обработки запроса"")
    finally:
        await callback.answer()");
            }

            // Генерация обработчиков товаров и заказов
            if (clientsCanMakeOrders)
            {
                sb.AppendLine(@"
# --- Обработчики товаров ---
@dp.message(F.text == ""Ассортимент"")
async def products_menu(message: types.Message):
    kb = get_products_keyboard()
    await message.answer(
        ""Выберите товар для заказа:"" if kb.inline_keyboard else ""Ассортимент пуст."",
        reply_markup=kb
    )

@dp.callback_query(F.data.startswith(""buy_""))
async def buy_product(callback: types.CallbackQuery, state: FSMContext):
    product_id = int(callback.data.split(""_"")[1])
    await callback.message.answer(""Введите количество:"")
    await state.update_data(product_id=product_id)
    await state.set_state(OrderStates.waiting_for_order_amount)

@dp.message(OrderStates.waiting_for_order_amount)
async def get_order_amount(message: types.Message, state: FSMContext):
    try:
        amount = float(message.text)
        if amount <= 0:
            raise ValueError
        await state.update_data(amount=amount)
        await message.answer(""Введите адрес доставки:"")
        await state.set_state(OrderStates.waiting_for_order_address)
    except ValueError:
        await message.answer(""Введите корректное количество"")

@dp.message(OrderStates.waiting_for_order_address)
async def get_order_address(message: types.Message, state: FSMContext):
    await state.update_data(address=message.text)
    await message.answer(""Введите телефон для связи:"")
    await state.set_state(OrderStates.waiting_for_order_phone)

@dp.message(OrderStates.waiting_for_order_phone)
async def finish_order(message: types.Message, state: FSMContext):
    data = await state.get_data()
    delivery_date = generate_delivery_date()
    cursor.execute(""INSERT OR IGNORE INTO users(user_id, is_admin) VALUES (?, ?)"",
               (message.from_user.id, int(is_admin(message.from_user.id))))
    cursor.execute(""""""
        INSERT INTO orders(
            user_id, product_id, delivery_date, 
            amount, address, phone
        ) VALUES (?, ?, ?, ?, ?, ?)
    """""", (
        message.from_user.id, data['product_id'], delivery_date,
        data['amount'], data['address'], message.text
    ))
    conn.commit()

    # Получаем информацию о заказе
    cursor.execute(""SELECT name, price FROM products WHERE id = ?"", (data['product_id'],))
    product_name, product_price = cursor.fetchone()
    total = product_price * data['amount']

    await message.answer(
        f""✅ Заказ оформлен!\n""
        f""Товар: {product_name}\n""
        f""Количество: {data['amount']}\n""
        f""Сумма: {total}₽\n""
        f""Адрес: {data['address']}\n""
        f""Телефон: {message.text}\n""
        f""Дата доставки: {delivery_date}""
    )");

                if (addNotifications)
                {
                    sb.AppendLine(@"
    # Уведомление админу
    await bot.send_message(
        ADMIN_ID,
        f""🛒 Новый заказ #{cursor.lastrowid}!\n""
        f""Клиент: {message.from_user.full_name} (ID: {message.from_user.id})\n""
        f""Товар: {product_name} x {data['amount']}\n""
        f""Сумма: {total}₽\n""
        f""Статус: new""
    )");
                }

                sb.AppendLine(@"    await state.clear()");

                // Обработчики заказов
                sb.AppendLine(@"
# --- Обработчики заказов ---
@dp.message(F.text == ""Мои заказы"")
async def user_orders(message: types.Message):
    user_id = message.from_user.id
    cursor.execute(""""""
        SELECT o.id, o.user_id, p.name, p.price, o.created_at 
        FROM orders o
        JOIN products p ON o.product_id = p.id
        WHERE o.user_id = ?
        ORDER BY o.created_at DESC
    """""", (user_id,))

    if orders := cursor.fetchall():
        table_text = format_orders_table(orders)
        await message.answer(f""Ваши заказы:\n{table_text}"", parse_mode=ParseMode.MARKDOWN_V2)
    else:
        await message.answer(""У вас пока нет заказов."")

@dp.message(F.text == ""Просмотреть заказы"")
async def admin_view_orders(message: types.Message):
    if not is_admin(message.from_user.id):
        await message.answer(""Доступ запрещён."")
        return

    await message.answer(
        ""Управление заказами:"",
        reply_markup=get_orders_management_keyboard()
    )");

                if (addSearchFilter)
                {
                    sb.AppendLine(@"
@dp.callback_query(F.data.startswith(""orders_""))
async def handle_orders_filter(callback: types.CallbackQuery, state: FSMContext):
    filter_type = callback.data.split(""_"")[1]
    query = None
    params = None
    if filter_type == ""all"":
        query = """"""
            SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            ORDER BY o.created_at DESC
        """"""
        params = ()
    elif filter_type == ""status"":
        status = callback.data.split(""_"")[2]
        query = """"""
            SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            WHERE o.status = ?
            ORDER BY o.created_at DESC
        """"""
        params = (status,)
    elif filter_type == ""by"":
        filter_by = callback.data.split(""_"")[2]
        if filter_by == ""date"":
            await callback.message.answer(""Введите дату в формате ГГГГ-ММ-ДД:"")
            await state.set_state(OrderStates.waiting_for_date_filter)
            return
        elif filter_by == ""price"":
            await callback.message.answer(""Введите минимальную сумму:"")
            await state.set_state(OrderStates.waiting_for_price_filter)
            return
        elif filter_by == ""product"":
            await callback.message.answer(""Введите название товара:"")
            await state.set_state(OrderStates.waiting_for_product_filter)
            return
        elif filter_by == ""user"":
            await callback.message.answer(""Введите ID пользователя:"")
            await state.set_state(OrderStates.waiting_for_user_filter)
            return
    cursor.execute(query, params)
    orders = cursor.fetchall()
    await show_orders_list(callback.message, orders)
    await callback.answer()

@dp.message(OrderStates.waiting_for_date_filter)
async def filter_by_date(message: types.Message, state: FSMContext):
    date = message.text
    cursor.execute(""""""
        SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
        FROM orders o
        JOIN products p ON o.product_id = p.id
        JOIN users u ON o.user_id = u.user_id
        WHERE date(o.created_at) = ?
        ORDER BY o.created_at DESC
    """""", (date,))
    await show_orders_list(message, cursor.fetchall())
    await state.clear()

@dp.message(OrderStates.waiting_for_price_filter)
async def filter_by_price(message: types.Message, state: FSMContext):
    try:
        min_price = float(message.text)
        cursor.execute(""""""
            SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            WHERE p.price >= ?
            ORDER BY p.price DESC
        """""", (min_price,))
        await show_orders_list(message, cursor.fetchall())
    except ValueError:
        await message.answer(""Введите корректную сумму"")
    await state.clear()

@dp.message(OrderStates.waiting_for_product_filter)
async def filter_by_product(message: types.Message, state: FSMContext):
    try:
        product_name = message.text
        cursor.execute(""""""
            SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            WHERE p.name = ?
            ORDER BY p.price DESC
        """""", (product_name,))
        await show_orders_list(message, cursor.fetchall())
    except ValueError:
        await message.answer(""Введите корректное название товара"")
    await state.clear()

@dp.message(OrderStates.waiting_for_user_filter)
async def filter_by_user(message: types.Message, state: FSMContext):
    try:
        user_id = int(message.text)
        cursor.execute(""""""
            SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            WHERE u.user_id = ?
            ORDER BY p.price DESC
        """""", (user_id,))
        await show_orders_list(message, cursor.fetchall())
    except ValueError:
        await message.answer(""Введите корректный user_id"")
    await state.clear()");
                }
                else
                {
                    sb.AppendLine(@"
@dp.callback_query(F.data.startswith(""orders_""))
async def handle_orders_filter(callback: types.CallbackQuery):
    filter_type = callback.data.split(""_"")[1]
    
    if filter_type == ""all"":
        cursor.execute(""""""
            SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            ORDER BY o.created_at DESC
        """""")
    elif filter_type == ""status"":
        status = callback.data.split(""_"")[2]
        cursor.execute(""""""
            SELECT o.id, u.user_id, p.name, p.price, o.created_at, o.status 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            WHERE o.status = ?
            ORDER BY o.created_at DESC
        """""", (status,))
    
    orders = cursor.fetchall()
    await show_orders_list(callback.message, orders)
    await callback.answer()");
                }

                sb.AppendLine(@"
async def show_orders_list(message: Union[types.Message, types.CallbackQuery], orders: list):
    if not orders:
        if isinstance(message, types.CallbackQuery):
            await message.message.answer(""Заказы не найдены"")
        else:
            await message.answer(""Заказы не найдены"")
        return

    status_icons = {
        'new': '🆕',
        'processing': '🔄',
        'shipped': '🚚',
        'delivered': '✅',
        'canceled': '❌'
    }

    text = ""Список заказов:\n\n""
    for order in orders:
        order_id, user_id, product_name, price, created_at, status = order
        text += (
            f""🆔 {order_id} | 👤 {user_id}\n""
            f""📦 {product_name} - {price}₽\n""
            f""📅 {created_at} | {status_icons.get(status, '')} {status}\n""
            f""————————————————\n""
        )

    reply_markup = get_orders_action_keyboard()

    if isinstance(message, types.CallbackQuery):
        await message.message.answer(text, reply_markup=reply_markup)
    else:
        await message.answer(text, reply_markup=reply_markup)

@dp.callback_query(F.data.startswith(""order_""))
async def handle_order_actions(callback: types.CallbackQuery, state: FSMContext):
    action = callback.data.split(""_"")[1]

    if action == ""edit"":
        await start_edit_order(callback, state)
    elif action == ""confirm"":
        await change_order_status(callback, ""delivered"", state)
    elif action == ""ship"":
        await change_order_status(callback, ""shipped"", state)
    elif action == ""cancel"":
        await change_order_status(callback, ""canceled"", state)
    elif action == ""details"":
        await show_order_details(callback, state)");

                if (addSearchFilter)
                {
                    sb.AppendLine(@"    elif action == ""filters"":
        await callback.message.answer(
            ""Фильтры заказов:"",
            reply_markup=get_orders_management_keyboard()
        )");
                }

                sb.AppendLine(@"
    await callback.answer()

async def change_order_status(callback: types.CallbackQuery, new_status: str, state: FSMContext):
    await callback.message.answer(
        ""Введите ID заказа для изменения статуса:"",
        reply_markup=types.ReplyKeyboardRemove()
    )
    await state.set_state(OrderStates.waiting_for_order_to_change)
    await state.update_data(new_status=new_status)

@dp.message(OrderStates.waiting_for_order_to_change)
async def process_order_status_change(message: types.Message, state: FSMContext):
    data = await state.get_data()
    new_status = data['new_status']

    try:
        order_id = int(message.text)
        cursor.execute(
            ""UPDATE orders SET status = ? WHERE id = ?"",
            (new_status, order_id)
        )
        conn.commit()

        cursor.execute(""""""
            SELECT o.user_id, p.name, u.user_id 
            FROM orders o
            JOIN products p ON o.product_id = p.id
            JOIN users u ON o.user_id = u.user_id
            WHERE o.id = ?
        """""", (order_id,))");

                if (addNotifications)
                {
                    sb.AppendLine(@"
        if order := cursor.fetchone():
            user_id, product_name, admin_id = order
            status_messages = {
                'delivered': ""доставленный"",
                'shipped': ""в доставку"",
                'canceled': ""отменен""
            }

            await bot.send_message(
                user_id,
                f""ℹ️ Статус вашего заказа #{order_id} ({product_name}) изменен: {status_messages[new_status]}""
            )");
                }

                sb.AppendLine(@"
        await message.answer(
            f""✅ Статус заказа #{order_id} изменен на '{new_status}'"",
            reply_markup=create_admin_keyboard()
        )
    except ValueError:
        await message.answer(""Введите корректный ID заказа"")
    finally:
        await state.clear()

async def show_order_details(callback: types.CallbackQuery, state: FSMContext):
    await callback.message.answer(""Введите ID заказа для просмотра:"")
    await state.set_state(OrderStates.wathing_order_by_order_id)
    await callback.answer()

@dp.message(OrderStates.wathing_order_by_order_id)
async def process_order_details(message: types.Message, state: FSMContext):
    try:
        order_id = int(message.text)
        cursor.execute(""""""
                   SELECT 
                       o.id, o.user_id, o.amount, o.address, o.phone, 
                       o.created_at, o.delivery_date, o.status,
                       p.name as product_name, p.price as product_price 
                   FROM orders o
                   JOIN products p ON o.product_id = p.id
                   WHERE o.id = ?
               """""", (order_id,))

        if order := cursor.fetchone():
            order_info = (
                f""📦 Заказ #{order[0]}\n""
                f""👤 Клиент: {order[1]}\n""
                f""🛒 Товар: {order[8]}\n""
                f""💰 Цена: {order[9]}₽ x {order[2]} = {order[9] * order[2]}₽\n""
                f""🏠 Адрес: {order[3]}\n""
                f""📞 Телефон: {order[4]}\n""
                f""📅 Дата создания: {order[5]}\n""
                f""🚚 Дата доставки: {order[6]}\n""
                f""🔄 Статус: {order[7]}""
            )

            await message.answer(order_info)
        else:
            await message.answer(""Заказ не найден"")
    except ValueError:
        await message.answer(""Введите корректный ID заказа"")
    finally:
        await state.clear()

@dp.callback_query(F.data == ""order_edit"")
async def start_edit_order(callback: types.CallbackQuery, state: FSMContext):
    await callback.message.answer(""Введите ID заказа для редактирования:"")
    await state.set_state(OrderStates.waiting_for_edit_order)
    await callback.answer()

@dp.message(OrderStates.waiting_for_edit_order)
async def select_order_to_edit(message: types.Message, state: FSMContext):
    try:
        order_id = int(message.text)
        cursor.execute(""SELECT * FROM orders WHERE id = ?"", (order_id,))
        if order := cursor.fetchone():
            await state.update_data(edit_order_id=order_id)
            await message.answer(
                ""Выберите поле для редактирования:"",
                reply_markup=InlineKeyboardMarkup(inline_keyboard=[
                    [InlineKeyboardButton(text=""Товар"", callback_data=""edit_field_product"")],
                    [InlineKeyboardButton(text=""Количество"", callback_data=""edit_field_amount"")],
                    [InlineKeyboardButton(text=""Адрес"", callback_data=""edit_field_address"")],
                    [InlineKeyboardButton(text = ""Телефон"", callback_data = ""edit_field_phone"")],
                    [InlineKeyboardButton(text = ""Статус"", callback_data = ""edit_field_status"")]
                ])
            )
            await state.set_state(OrderStates.waiting_for_edit_field)
        else:
            await message.answer(""Заказ не найден"")
    except ValueError:
        await message.answer(""Введите корректный ID заказа"")

@dp.callback_query(OrderStates.waiting_for_edit_field, F.data.startswith(""edit_field_""))
async def select_field_to_edit(callback: types.CallbackQuery, state: FSMContext):
    field = callback.data.split(""_"")[2]
    await state.update_data(edit_field = field)
    await callback.message.answer(f""Введите новое значение для поля { field}:"")
    await state.set_state(OrderStates.waiting_for_edit_value)
    await callback.answer()

@dp.message(OrderStates.waiting_for_edit_value)
async def save_edited_field(message: types.Message, state: FSMContext):
    data = await state.get_data()
    order_id = data['edit_order_id']
    field = data['edit_field']
    value = message.text
                    
    try:
        if field == ""amount"":
            value = float(value)
        elif field == ""product"":
            cursor.execute(""SELECT id FROM products WHERE name = ? "", (value,))
            if product := cursor.fetchone():
                value = product[0]
            else:
                await message.answer(""Товар не найден"")
                return

        cursor.execute(f""UPDATE orders SET { field} = ? WHERE id = ? "", (value, order_id))
        conn.commit()
        await message.answer(""Заказ успешно обновлён"")
    except Exception as e:
        await message.answer(f""Ошибка: { str(e)}"")
    finally:
        await state.clear()");
            }

            // Обработчики обращений
            sb.AppendLine(@"
# --- Обработчики обращений ---
@dp.message(F.text == ""Мои обращения"")
async def user_messages(message: types.Message):
    user_id = message.from_user.id
    cursor.execute(""""""
        SELECT id, message, created_at 
        FROM messages 
        WHERE user_id = ? 
        ORDER BY created_at DESC
    """""", (user_id,))

    if rows := cursor.fetchall():
        text = ""Ваши обращения:\n"" + ""\n"".join(f""{dt}: {msg}"" for mid, msg, dt in rows)
        await message.answer(text)
    else:
        await message.answer(""У вас пока нет обращений."")

@dp.message(F.text == ""Просмотреть обращения"")
async def admin_view_messages(message: types.Message):
    if not is_admin(message.from_user.id):
        await message.answer(""Доступ запрещён."")
        return

    cursor.execute(""""""
        SELECT messages.id, users.user_id, messages.message, messages.created_at
        FROM messages
        JOIN users ON messages.user_id = users.user_id
        ORDER BY messages.created_at DESC
        LIMIT 50
    """""")

    if rows := cursor.fetchall():
        text = ""Обращения пользователей:\n"" + ""\n"".join(
            f""ID:{mid} User:{uid} at {dt}\n{msg}\n""
            for mid, uid, msg, dt in rows
        )
        await message.answer(text)
    else:
        await message.answer(""Обращений нет."")

@dp.message(MessageStates.waiting_for_message)
async def support_message(message: types.Message, state: FSMContext):
    user_id = message.from_user.id
    cursor.execute(""INSERT INTO messages(user_id, message) VALUES (?, ?)"",
               (user_id, message.text))
    conn.commit()
    await message.answer(""Ваше обращение зарегистрировано. Мы свяжемся с вами."")
    await state.clear()");

            // Админские обработчики FAQ
            if (addFAQ)
            {
                sb.AppendLine(@"
# --- Админские обработчики FAQ ---
@dp.message(F.text == ""Редактировать FAQ"")
async def admin_faq(message: types.Message):
    if not is_admin(message.from_user.id):
        await message.answer(""Доступ запрещён."")
        return

    buttons = [
        [InlineKeyboardButton(text=""Добавить вопрос"", callback_data=""faq_add"")],
        [InlineKeyboardButton(text=""Редактировать вопрос"", callback_data=""faq_edit"")],
        [InlineKeyboardButton(text=""Удалить вопрос"", callback_data=""faq_delete"")]
    ]
    await message.answer(
        ""Выберите действие с FAQ:"",
        reply_markup=InlineKeyboardMarkup(inline_keyboard=buttons)
    )

@dp.callback_query(F.data == ""faq_add"")
async def faq_add_handler(callback: types.CallbackQuery, state: FSMContext):
    await callback.message.answer(""Введите вопрос для FAQ:"")
    await state.set_state(FAQStates.waiting_for_question)
    await callback.answer()

@dp.message(FAQStates.waiting_for_question)
async def faq_get_question(message: types.Message, state: FSMContext):
    await state.update_data(faq_question=message.text)
    await message.answer(""Введите ответ на вопрос:"")
    await state.set_state(FAQStates.waiting_for_answer)

@dp.message(FAQStates.waiting_for_answer)
async def faq_get_answer(message: types.Message, state: FSMContext):
    data = await state.get_data()
    question = data.get(""faq_question"")
    answer = message.text
    cursor.execute(""INSERT INTO faq(question, answer) VALUES (?, ?)"", (question, answer))
    conn.commit()
    await message.answer(""Вопрос и ответ добавлены в FAQ."")
    await state.clear()

@dp.callback_query(F.data == ""faq_edit"")
async def faq_edit_handler(callback: types.CallbackQuery):
    cursor.execute(""SELECT id, question FROM faq"")
    faqs = cursor.fetchall()
    if not faqs:
        await callback.message.answer(""FAQ пуст."")
        await callback.answer()
        return

    buttons = []
    for fid, q in faqs:
        buttons.append([InlineKeyboardButton(text=q, callback_data=f""faq_edit_{fid}"")])

    kb = InlineKeyboardMarkup(inline_keyboard=buttons)
    await callback.message.answer(""Выберите вопрос для редактирования:"", reply_markup=kb)
    await callback.answer()

@dp.callback_query(F.data.startswith(""faq_edit_""))
async def faq_edit_select(callback: types.CallbackQuery, state: FSMContext):
    try:
        faq_id = int(callback.data.split(""_"")[2])
        await state.update_data(faq_edit_id=faq_id)
        await callback.message.answer(""Введите новый вопрос:"")
        await state.set_state(FAQStates.waiting_for_edit_question)
    except (ValueError, IndexError):
        await callback.answer(""Ошибка обработки запроса"")
    finally:
        await callback.answer()

@dp.message(FAQStates.waiting_for_edit_question)
async def faq_get_edit_question(message: types.Message, state: FSMContext):
    await state.update_data(faq_edit_question=message.text)
    await message.answer(""Введите новый ответ:"")
    await state.set_state(FAQStates.waiting_for_edit_answer)

@dp.message(FAQStates.waiting_for_edit_answer)
async def faq_get_edit_answer(message: types.Message, state: FSMContext):
    data = await state.get_data()
    faq_id = data.get(""faq_edit_id"")
    question = data.get(""faq_edit_question"")
    answer = message.text
    cursor.execute(""UPDATE faq SET question = ?, answer = ? WHERE id = ?"", (question, answer, faq_id))
    conn.commit()
    await message.answer(""Вопрос и ответ обновлены."")
    await state.clear()

@dp.callback_query(F.data == ""faq_delete"")
async def faq_delete_handler(callback: types.CallbackQuery):
    cursor.execute(""SELECT id, question FROM faq"")
    faqs = cursor.fetchall()
    if not faqs:
        await callback.message.answer(""FAQ пуст."")
        await callback.answer()
        return

    buttons = []
    for fid, q in faqs:
        buttons.append([InlineKeyboardButton(text=q, callback_data=f""faq_delete_{fid}"")])

    kb = InlineKeyboardMarkup(inline_keyboard=buttons)
    await callback.message.answer(""Выберите вопрос для удаления:"", reply_markup=kb)
    await callback.answer()

@dp.callback_query(F.data.startswith(""faq_delete_""))
async def faq_delete_confirm(callback: types.CallbackQuery):
    try:
        faq_id = int(callback.data.split(""_"")[2])
        cursor.execute(""DELETE FROM faq WHERE id = ?"", (faq_id,))
        conn.commit()
        await callback.message.answer(""Вопрос удалён."")
    except (ValueError, IndexError):
        await callback.answer(""Ошибка обработки запроса"")
    finally:
        await callback.answer()");
            }

            // Админские обработчики продуктов
            if (clientsCanMakeOrders)
            {
                sb.AppendLine(@"
# --- Админские обработчики продуктов ---
@dp.message(F.text == ""Редактировать товары"")
async def admin_products(message: types.Message):
    if not is_admin(message.from_user.id):
        await message.answer(""Доступ запрещён."")
        return

    buttons = [
        [InlineKeyboardButton(text=""Добавить товар"", callback_data=""prod_add"")],
        [InlineKeyboardButton(text=""Редактировать товар"", callback_data=""prod_edit"")],
        [InlineKeyboardButton(text=""Удалить товар"", callback_data=""prod_delete"")]
    ]
    kb = InlineKeyboardMarkup(inline_keyboard=buttons)
    await message.answer(""Выберите действие с товарами:"", reply_markup=kb)

@dp.callback_query(F.data == ""prod_add"")
async def product_add(callback: types.CallbackQuery, state: FSMContext):
    await callback.message.answer(""Введите название товара:"")
    await state.set_state(ProductStates.waiting_for_name)
    await callback.answer()

@dp.message(ProductStates.waiting_for_name)
async def product_get_name(message: types.Message, state: FSMContext):
    await state.update_data(prod_name=message.text)
    await message.answer(""Введите описание товара:"")
    await state.set_state(ProductStates.waiting_for_description)

@dp.message(ProductStates.waiting_for_description)
async def product_get_description(message: types.Message, state: FSMContext):
    await state.update_data(prod_description=message.text)
    await message.answer(""Введите цену товара (число):"")
    await state.set_state(ProductStates.waiting_for_price)

@dp.message(ProductStates.waiting_for_price)
async def product_get_price(message: types.Message, state: FSMContext):
    try:
        price = float(message.text)
    except ValueError:
        await message.answer(""Введите корректное число для цены."")
        return
    data = await state.get_data()
    name = data.get(""prod_name"")
    description = data.get(""prod_description"")
    cursor.execute(""INSERT INTO products(name, description, price) VALUES (?, ?, ?)"", (name, description, price))
    conn.commit()
    await message.answer(""Товар добавлен."")
    await state.clear()

@dp.callback_query(F.data == ""prod_edit"")
async def product_edit_list(callback: types.CallbackQuery):
    cursor.execute(""SELECT id, name FROM products"")
    products = cursor.fetchall()
    if not products:
        await callback.message.answer(""Товары отсутствуют."")
        await callback.answer()
        return

    buttons = []
    for pid, name in products:
        buttons.append([InlineKeyboardButton(text=name, callback_data=f""prod_edit_id_{pid}"")])

    kb = InlineKeyboardMarkup(inline_keyboard=buttons)
    await callback.message.answer(""Выберите товар для редактирования:"", reply_markup=kb)
    await callback.answer()

@dp.callback_query(F.data.startswith(""prod_edit_id_""))
async def product_edit_id(callback: types.CallbackQuery, state: FSMContext):
    prod_id = int(callback.data.split(""_"")[-1])
    await state.update_data(prod_edit_id=prod_id)
    await callback.message.answer(""Введите новое название товара:"")
    await state.set_state(ProductStates.waiting_for_edit_name)
    await callback.answer()

@dp.message(ProductStates.waiting_for_edit_name)
async def product_edit_name(message: types.Message, state: FSMContext):
    await state.update_data(prod_edit_name=message.text)
    await message.answer(""Введите новое описание товара:"")
    await state.set_state(ProductStates.waiting_for_edit_description)

@dp.message(ProductStates.waiting_for_edit_description)
async def product_edit_description(message: types.Message, state: FSMContext):
    await state.update_data(prod_edit_description=message.text)
    await message.answer(""Введите новую цену товара:"")
    await state.set_state(ProductStates.waiting_for_edit_price)

@dp.message(ProductStates.waiting_for_edit_price)
async def product_edit_price(message: types.Message, state: FSMContext):
    try:
        price = float(message.text)
    except ValueError:
        await message.answer(""Введите корректное число для цены."")
        return
    data = await state.get_data()
    prod_id = data.get(""prod_edit_id"")
    name = data.get(""prod_edit_name"")
    description = data.get(""prod_edit_description"")
    cursor.execute(""UPDATE products SET name = ?, description = ?, price = ? WHERE id = ?"",
               (name, description, price, prod_id))
    conn.commit()
    await message.answer(""Товар обновлён."")
    await state.clear()

@dp.callback_query(F.data == ""prod_delete"")
async def product_delete_list(callback: types.CallbackQuery):
    cursor.execute(""SELECT id, name FROM products"")
    products = cursor.fetchall()
    if not products:
        await callback.message.answer(""Товары отсутствуют."")
        await callback.answer()
        return

    buttons = []
    for pid, name in products:
        buttons.append([InlineKeyboardButton(text=name, callback_data=f""prod_delete_id_{pid}"")])

    kb = InlineKeyboardMarkup(inline_keyboard=buttons)
    await callback.message.answer(""Выберите товар для удаления:"", reply_markup=kb)
    await callback.answer()

@dp.callback_query(F.data.startswith(""prod_delete_id_""))
async def product_delete_id(callback: types.CallbackQuery):
    prod_id = int(callback.data.split(""_"")[-1])
    cursor.execute(""DELETE FROM products WHERE id = ?"", (prod_id,))
    conn.commit()
    await callback.message.answer(""Товар удалён."")
    await callback.answer()");
            }

            // Обработчики расходов
            if (botTracksExpenses)
            {
                sb.AppendLine(@"
# --- Обработчики расходов ---
@dp.message(F.text == ""Добавить расходы"")
async def add_expense_start(message: types.Message, state: FSMContext):
    if not is_admin(message.from_user.id):
        await message.answer(""Доступ запрещён."")
        return

    await message.answer(""Введите название товара/услуги:"")
    await state.set_state(ExpenseStates.waiting_for_name)

@dp.message(ExpenseStates.waiting_for_name)
async def get_expense_name(message: types.Message, state: FSMContext):
    await state.update_data(expense_name=message.text)
    await message.answer(""Введите количество:"")
    await state.set_state(ExpenseStates.waiting_for_amount)

@dp.message(ExpenseStates.waiting_for_amount)
async def get_expense_amount(message: types.Message, state: FSMContext):
    try:
        amount = float(message.text)
        await state.update_data(expense_amount=amount)
        await message.answer(""Введите общую стоимость:"")
        await state.set_state(ExpenseStates.waiting_for_price)
    except ValueError:
        await message.answer(""Пожалуйста, введите число."")

@dp.message(ExpenseStates.waiting_for_price)
async def get_expense_price(message: types.Message, state: FSMContext):
    try:
        price = float(message.text)
        data = await state.get_data()
        name = data.get('expense_name')
        amount = data.get('expense_amount')

        cursor.execute(
            ""INSERT INTO expenses (name, amount, price) VALUES (?, ?, ?)"",
            (name, amount, price)
        )
        conn.commit()

        await message.answer(f""Расход добавлен:\n{name} - {amount} шт. на сумму {price}₽"")
        await state.clear()
    except ValueError:
        await message.answer(""Пожалуйста, введите число."")

@dp.message(F.text == ""Просмотреть расходы"")
async def view_expenses_menu(message: types.Message):
    if not is_admin(message.from_user.id):
        await message.answer(""Доступ запрещён."")
        return

    now = datetime.now()
    current_month = now.month
    current_year = now.year

    keyboard = InlineKeyboardMarkup(inline_keyboard=[
        [
            InlineKeyboardButton(text=""Текущий месяц"", callback_data=f""expenses_{current_year}_{current_month}""),
            InlineKeyboardButton(text=""Прошлый месяц"", callback_data=f""expenses_{current_year}_{current_month - 1}"")
        ],
        [
            InlineKeyboardButton(text=""Все расходы"", callback_data=""expenses_all""),
            InlineKeyboardButton(text=""Выбрать месяц"", callback_data=""expenses_choose"")
        ]
    ])

    await message.answer(""Выберите период для просмотра расходов:"", reply_markup=keyboard)

@dp.callback_query(F.data.startswith(""expenses_""))
async def show_expenses(callback: types.CallbackQuery):
    data = callback.data.split(""_"")

    if data[1] == ""all"":
        cursor.execute(""""""
            SELECT name, amount, price, created_at 
            FROM expenses 
            ORDER BY created_at DESC
        """""")
        expenses = cursor.fetchall()
        title = ""Все расходы:""
    elif data[1] == ""choose"":
        await callback.message.answer(""Введите месяц и год в формате ММ.ГГГГ (например 05.2024):"")
        await ExpenseStates.waiting_for_month.set()
        await callback.answer()
        return
    else:
        year = int(data[1])
        month = int(data[2])
        cursor.execute(""""""
            SELECT name, amount, price, created_at 
            FROM expenses 
            WHERE year = ? AND month = ?
            ORDER BY created_at DESC
        """""", (year, month))
        expenses = cursor.fetchall()
        title = f""Расходы за {month}.{year}:""

    if not expenses:
        await callback.message.answer(""Нет расходов за выбранный период."")
        await callback.answer()
        return

    total = 0
    expense_list = []
    for name, amount, price, date in expenses:
        expense_list.append(f""{date[:10]}: {name} - {amount} шт. на {price}₽"")
        total += price

    expense_text = ""\n"".join(expense_list)
    response = f""{title}\n\n{expense_text}\n\nИтого: {total}₽""

    await callback.message.answer(response)
    await callback.answer()

@dp.message(ExpenseStates.waiting_for_month)
async def get_expenses_by_custom_month(message: types.Message, state: FSMContext):
    try:
        month, year = map(int, message.text.split('.'))
        if month < 1 or month > 12:
            raise ValueError

        cursor.execute(""""""
            SELECT name, amount, price, created_at 
            FROM expenses 
            WHERE year = ? AND month = ?
            ORDER BY created_at DESC
        """""", (year, month))
        expenses = cursor.fetchall()

        if not expenses:
            await message.answer(f""Нет расходов за {month}.{year}."")
            await state.clear()
            return

        total = 0
        expense_list = []
        for name, amount, price, date in expenses:
            expense_list.append(f""{date[:10]}: {name} - {amount} шт. на {price}₽"")
            total += price

        expense_text = ""\n"".join(expense_list)
        response = f""Расходы за {month}.{year}:\n\n{expense_text}\n\nИтого: {total}₽""

        await message.answer(response)
        await state.clear()
    except (ValueError, IndexError):
        await message.answer(""Неверный формат. Введите месяц и год в формате ММ.ГГГГ (например 05.2024):"")");
            }

            // Статистика прибыли
            sb.AppendLine(@"
# --- Статистика прибыли ---
@dp.message(F.text == ""Статистика прибыли"")
async def show_profit_stats(message: types.Message):
    if not is_admin(message.from_user.id):
        await message.answer(""Доступ запрещён."")
        return

    cursor.execute(""""""
        SELECT year, month FROM (
            SELECT year, month FROM expenses
            UNION
            SELECT CAST(strftime('%Y', created_at) AS INTEGER) AS year,
                   CAST(strftime('%m', created_at) AS INTEGER) AS month
            FROM orders
        )
        ORDER BY year DESC, month DESC
    """""")
    months = cursor.fetchall()
    if not months:
        await message.answer(""Данных для статистики нет."")
        return

    header = [""📅 Месяц"", ""Расходы (₽)"", ""Доходы (₽)"", ""Прибыль (₽)""]
    table_data = []

    for year_int, month_int in months:
        cursor.execute(""""""
            SELECT IFNULL(SUM(price), 0) FROM expenses 
            WHERE year = ? AND month = ?
        """""", (year_int, month_int))
        expenses_sum = cursor.fetchone()[0] or 0

        cursor.execute(""""""
            SELECT IFNULL(SUM(p.price * o.amount), 0)
            FROM orders o
            JOIN products p ON o.product_id = p.id
            WHERE strftime('%Y', o.created_at) = ? 
              AND strftime('%m', o.created_at) = ? 
              AND o.status = 'delivered'
        """""", (f""{year_int}"", f""{month_int:02d}""))
        income_sum = cursor.fetchone()[0] or 0

        profit = income_sum - expenses_sum

        table_data.append([
            f""{year_int}-{month_int:02d}"",
            f""{expenses_sum:,.2f}"",
            f""{income_sum:,.2f}"",
            f""{profit:,.2f}""
        ])");

            if (trackOrdersInTable)
            {
                sb.AppendLine(@"
    report_text = ""```\n"" + tabulate.tabulate(table_data, headers=header, tablefmt=""plain"") + ""\n```""

    await message.answer(
        ""📊 Статистика прибыли по месяцам:\n"" + report_text,
        parse_mode=ParseMode.MARKDOWN
    )");
            }
            else
            {
                sb.AppendLine(@"
    report_text = ""📊 Статистика прибыли по месяцам:\n\n""
    report_text += "" | "".join(header) + ""\n""
    for row in table_data:
        report_text += "" | "".join(row) + ""\n""

    await message.answer(report_text)");
            }

            // Проверка и отправка уведомлений о доставке
            if (addNotifications && clientsCanMakeOrders)
            {
                sb.AppendLine(@"
# --- Проверка и отправка уведомлений о доставке ---
async def check_deliveries():
    now = datetime.now().strftime(""%Y-%m-%d %H:%M:%S"")
    cursor.execute(""""""
        SELECT o.id, o.user_id, p.name, u.user_id 
        FROM orders o
        JOIN products p ON o.product_id = p.id
        JOIN users u ON o.user_id = u.user_id
        WHERE o.delivery_date <= ? AND o.status = 'shipped'
    """""", (now,))

    deliveries = cursor.fetchall()
    for order_id, user_id, product_name, admin_id in deliveries:
        await bot.send_message(
            user_id,
            f""🎉 Ваш заказ #{order_id} ({product_name}) доставлен!""
        )
        await bot.send_message(
            admin_id,
            f""✅ Заказ #{order_id} доставлен пользователю {user_id}""
        )
        cursor.execute(""UPDATE orders SET status = 'delivered' WHERE id = ?"", (order_id,))
        conn.commit()");
            }

            // Периодическая проверка доставок
            if (addNotifications && clientsCanMakeOrders)
            {
                sb.AppendLine(@"
# --- Периодическая проверка доставок ---
async def delivery_checker():
    while True:
        await check_deliveries()
        await asyncio.sleep(3600)  # Проверяем каждый час

async def on_startup():
    asyncio.create_task(delivery_checker())");
            }

            // Запуск бота
            sb.AppendLine(@"
# --- Запуск бота ---
if __name__ == ""__main__"":
    init_db()");

            if (addNotifications && clientsCanMakeOrders)
            {
                sb.AppendLine("    dp.startup.register(on_startup)");
            }

            sb.AppendLine(@"    asyncio.run(dp.start_polling(bot))");

            return sb.ToString();
        }
    }
}


