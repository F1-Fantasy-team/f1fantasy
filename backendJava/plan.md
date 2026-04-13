The contents of this folder should seek to reproduce the .net api in '../backend/' including, but not limited to: 
    - api functionality, internally and externally
    - calculation, standings, data fetching
    - All middleware: Client caching, global exception handling, ip blacklisting, request context loading, external api polling rate and pagination handling.
    - tdd approach
    - use the same .env/secrets structure

tech: Java 21 w/ Spring Boot
In addition we want to allow for the opportunity to listen to a CRON job that triggers kafka events. It's job is to periodically query the source data and fire an even whenever there is an update.

core principles:
We write tests for all logic, we test the logic and verify, we assure and verify ahead of time that we don't introduce persistent state into our database, meaning we clean-up test data, even if the program crashes.