-- SnelStart subscription key wordt voortaan uitsluitend uit configuratie gelezen
-- (SnelStartGlobal:SubscriptionKey in appsettings.Production.json). Hij wordt niet
-- langer per tenant of per bankrekening versleuteld opgeslagen.
--
-- Deze migratie maakt de bestaande, nu ongebruikte kolommen leeg zodat er geen
-- verouderde secrets meer 'at rest' staan. De kolommen en de stored-procedure-
-- parameters blijven voorlopig bestaan (de repositories geven simpelweg NULL door);
-- de definitieve verwijdering staat onderaan als losse, nog niet uitgevoerde stap.

SET NOCOUNT ON;

UPDATE dbo.TenantSnelStartConnections
SET SubscriptionKeyEncrypted = NULL
WHERE SubscriptionKeyEncrypted IS NOT NULL;
GO

UPDATE dbo.BankAccountSettings
SET SnelStartSubscriptionKeyEncrypted = NULL
WHERE SnelStartSubscriptionKeyEncrypted IS NOT NULL;
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- LATERE OPRUIMING (NIET UITVOEREN tot bevestigd is dat geen enkele omgeving of
-- oudere app-versie deze kolommen/parameters meer gebruikt):
--
--   1. Pas de stored procedures aan zodat de parameters en kolomverwijzingen
--      verdwijnen:
--        - dbo.TenantSnelStartConnections_Insert   (@SubscriptionKeyEncrypted)
--        - dbo.TenantSnelStartConnections_Update   (@SubscriptionKeyEncrypted)
--        - dbo.TenantSnelStartConnections_GetByTenantId / _GetById /
--          _GetExpiringCustomKey  (SELECT-lijst)
--        - dbo.BankAccountSettings_Insert / _Update / _GetByBankAccountId /
--          _GetById  (@SnelStartSubscriptionKeyEncrypted resp. SELECT-lijst)
--      en werk de bijbehorende repository-code bij.
--
--   2. Verwijder daarna de kolommen:
--        ALTER TABLE dbo.TenantSnelStartConnections DROP COLUMN SubscriptionKeyEncrypted;
--        ALTER TABLE dbo.BankAccountSettings        DROP COLUMN SnelStartSubscriptionKeyEncrypted;
-- ───────────────────────────────────────────────────────────────────────────────
