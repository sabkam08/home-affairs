DEF-006

Description:
Automated account-lockout integration testing identified that an
account could successfully authenticate after five consecutive failed
login attempts instead of remaining temporarily locked.

Detected by:
Five_failed_logins_should_lock_account

Priority:
High

Root cause:
Failed-login threshold and lockout state were calculated within the
same SQL UPDATE expression, resulting in incorrect threshold behavior.

Resolution:
[identity].User_RecordFailedLogin was changed to calculate the next
failed-attempt count and lockout decision explicitly before updating
[identity].Users.

Verification:
Automated integration test verifies:
- five invalid passwords return 401;
- LockedUntilUtc is in the future;
- FailedLoginAttempts resets after lockout;
- ACCOUNT_LOCKED is recorded;
- correct credentials remain rejected while the lock is active.

Status:
Fixed


DEF-007

Description:
TRX evidence-generation run caused SQL Server Testcontainers database
provisioning to time out, resulting in all integration cases being
reported as failed before their test bodies executed.

Priority:
Medium

Type:
Automated test infrastructure

Resolution:
- Integration tests configured for deterministic non-parallel execution.
- Test database schema provisioning timeout increased to 120 seconds.
- TRX results generated per test project rather than sharing and
  overwriting HomeAffairs.Tests.trx.

Application impact:
None. Previous complete backend test execution passed.

Status:
Fixed once the separated TRX runs are verified.
