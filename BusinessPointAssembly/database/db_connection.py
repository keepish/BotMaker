import os
import pyodbc
import asyncio
from dotenv import load_dotenv
from typing import Optional, AsyncGenerator

class DatabasePool:
    def __init__(self):
        load_dotenv()
        self.pool: Optional[asyncio.Queue] = None
        self.max_connections = int(os.getenv('MAX_DB_CONNECTIONS', 5))
        self.connectionString = f"""
        DRIVER={{{os.getenv('DRIVER_NAME')}}};
        SERVER={os.getenv('SERVER_NAME')};
        DATABASE={os.getenv('DATABASE_NAME')};
        UID={os.getenv('USER_NAME')};
        PWD={os.getenv('PASSWORD')};
        TrustServerCertificate=yes;"""

    async def init_pool(self):
        if self.pool is None:
            self.pool = asyncio.Queue(maxsize=self.max_connections)
            for _ in range(self.max_connections):
                connection = pyodbc.connect(self.connectionString, autocommit=False)
                self.pool.put_nowait(connection)

    async def get_cursor(self) -> AsyncGenerator[pyodbc.Cursor, None]:
        if self.pool is None:
            await self.init_pool()

        connection = await self.pool.get()
        try:
            cursor = connection.cursor()
            yield cursor
            connection.commit()
        except Exception as e:
            connection.rollback()
            raise e
        finally:
            self.pool.put_nowait(connection)

    async def close_pool(self):
        if self.pool is not None:
            while not self.pool.empty():
                connection = await self.pool.get()
                connection.close()
            self.pool = None

db_pool = DatabasePool()
