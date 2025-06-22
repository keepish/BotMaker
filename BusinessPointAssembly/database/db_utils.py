import logging
from database.db_connection import db_pool

logger = logging.getLogger(__name__)

async def Connect():
    # Ensure the pool is initialized
    if not db_pool.pool:
        await db_pool.init_pool()

#region ---User---
async def IsUser(UserId: int) -> bool:
    async for cursor in db_pool.get_cursor():
        cursor.execute("SELECT UserId FROM [Users] WHERE UserId = ?", UserId)
        result = cursor.fetchone()
        if result:
            return result.UserId
        return None
    
async def IsUserVip(UserId: int) -> bool:
    async for cursor in db_pool.get_cursor():
        cursor.execute("SELECT isVip FROM [Users] WHERE UserId = ?", UserId)
        result = cursor.fetchone()
        if result:
            return result.isVip
        return None

async def GetFillingStatus(UserId: int) -> bool:
    async for cursor in db_pool.get_cursor():
        cursor.execute("SELECT IsFilling FROM [Users] WHERE UserId = ?", UserId)
        result = cursor.fetchone()
        return result.IsFilling

async def AddUser(UserId: int, Name: str):
    async for cursor in db_pool.get_cursor():
        if Name is None:
            Name = UserId
        cursor.execute("INSERT INTO [Users](UserId, Name) VALUES(?, ?)", UserId, Name)
        logger.info(f"---{Name} was inserted into table User---")

async def UpdateUser(UserId: int, Name: str):
    async for cursor in db_pool.get_cursor():
        if Name is None:
            Name = UserId
        cursor.execute("UPDATE [Users] SET Name = ? WHERE UserId = ?", Name, UserId)
        logger.info(f"---{Name} was updated in table User---")

async def SetVip(UserId: int):
    async for cursor in db_pool.get_cursor():
        cursor.execute("UPDATE [Users] SET isVip = 1 WHERE UserId = ?", UserId)

async def BanUser(UserId: int):
    async for cursor in db_pool.get_cursor():
        cursor.execute("UPDATE [Users] SET IsBanned = 1 WHERE UserId = ?", UserId)

async def UnBanUser(UserId: int):
    async for cursor in db_pool.get_cursor():
        cursor.execute("UPDATE [Users] SET IsBanned = 0 WHERE UserId = ?", UserId)

async def UpdateFillingStatus(UserId: int):
    async for cursor in db_pool.get_cursor():
        cursor.execute("UPDATE [Users] SET IsFilling = ~ IsFilling WHERE UserId = ?", UserId)
#endregion

#region ---Bot---
async def AddBot(UserId: int, Token: str, Name: str):
    async for cursor in db_pool.get_cursor():
        cursor.execute("INSERT INTO [Bots](UserId, Token, Name) VALUES(?, ?, ?)", UserId, Token, Name)

async def GetBots(UserId: int) -> dict:
    async for cursor in db_pool.get_cursor():
        cursor.execute("SELECT Token, Name FROM [Bots] WHERE UserId = ?", UserId)
        result = cursor.fetchall()
        return result
    
async def GetBotByToken(Token: str) -> dict:
    async for cursor in db_pool.get_cursor():
        cursor.execute("SELECT UserId, Token, Name FROM [Bots] WHERE Token = ?", Token)
        result = cursor.fetchone()
        return result

async def DeleteBot(UserId: int, Token: str):
    async for cursor in db_pool.get_cursor():
        cursor.execute("DELETE FROM [Bots] WHERE UserId = ? AND Token = ?", UserId, Token)
#endregion

#region ---Media, KeyboardMarkup---
async def GetMedia(MediaName: str) -> str:
    async for cursor in db_pool.get_cursor():
        cursor.execute(f"SELECT MediaCode FROM [Media] WHERE MediaName = ?", MediaName)
        result = cursor.fetchone()
        return result.MediaCode

async def GetKeyboardMarkup(MarkupName: str) -> int:
    async for cursor in db_pool.get_cursor():
        cursor.execute(f"SELECT Markup FROM [KeyboardMarkup] WHERE Name = ?", MarkupName)
        result = cursor.fetchone()
        markups = list(map(int, str(result.Markup)))
        return markups
#endregion