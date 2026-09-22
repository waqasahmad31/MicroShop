#!/usr/bin/env bash
set -euo pipefail

# The official entrypoint runs this only when PGDATA is empty.
# During initialization only its Unix socket is available; TCP starts after all scripts succeed.
# psql reads passwords from environment, quotes them as SQL literals and never echoes them.
psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --no-psqlrc --set ON_ERROR_STOP=1 <<'SQL'
\getenv identity_password IDENTITY_DB_PASSWORD
\getenv catalog_password CATALOG_DB_PASSWORD
\getenv inventory_password INVENTORY_DB_PASSWORD
\getenv ordering_password ORDERING_DB_PASSWORD

CREATE ROLE identity_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD :'identity_password';
CREATE ROLE catalog_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD :'catalog_password';
CREATE ROLE inventory_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD :'inventory_password';
CREATE ROLE ordering_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD :'ordering_password';

CREATE DATABASE identity_db OWNER identity_app;
CREATE DATABASE catalog_db OWNER catalog_app;
CREATE DATABASE inventory_db OWNER inventory_app;
CREATE DATABASE ordering_db OWNER ordering_app;

-- Database ownership alone does not remove PostgreSQL's default PUBLIC CONNECT privilege.
REVOKE ALL ON DATABASE identity_db, catalog_db, inventory_db, ordering_db FROM PUBLIC;
GRANT ALL ON DATABASE identity_db TO identity_app;
GRANT ALL ON DATABASE catalog_db TO catalog_app;
GRANT ALL ON DATABASE inventory_db TO inventory_app;
GRANT ALL ON DATABASE ordering_db TO ordering_app;
REVOKE ALL ON DATABASE postgres, template1 FROM PUBLIC;

\connect identity_db
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT ALL ON SCHEMA public TO identity_app;
\connect catalog_db
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT ALL ON SCHEMA public TO catalog_app;
\connect inventory_db
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT ALL ON SCHEMA public TO inventory_app;
\connect ordering_db
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT ALL ON SCHEMA public TO ordering_app;
SQL

echo "MicroShop: four service databases and restricted owner roles initialized; no application tables."
