import asyncio
import logging
import os
from dotenv import load_dotenv
from aiogram import Bot, Dispatcher
from app.handlers import router
from database.db_connection import db_pool

async def main():
    # Load environment variables
    load_dotenv()

    # Initialize database connection pool
    await db_pool.init_pool()

    # Initialize bot and dispatcher
    bot = Bot(token=os.getenv('TOKEN'))
    dp = Dispatcher()
    dp.include_router(router)

    # Start polling
    try:
        await dp.start_polling(bot)
    finally:
        # Clean up database connections
        await db_pool.close_pool()

if __name__ == '__main__':
    logging.basicConfig(level=logging.INFO)
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("Exit")
