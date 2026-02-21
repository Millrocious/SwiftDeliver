IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SwiftDeliver_Auth')
BEGIN
    CREATE DATABASE [SwiftDeliver_Auth]
END
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SwiftDeliver_Orders')
BEGIN
    CREATE DATABASE [SwiftDeliver_Orders]
END
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SwiftDeliver_Tracking')
BEGIN
    CREATE DATABASE [SwiftDeliver_Tracking]
END
GO