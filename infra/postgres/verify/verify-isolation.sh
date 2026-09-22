#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C
export PGCONNECT_TIMEOUT=5

# Force TCP so the checks exercise password authentication, not trusted Unix-socket access.
for service in identity catalog inventory ordering; do
    password_variable="${service^^}_DB_PASSWORD"
    export PGPASSWORD="${!password_variable}"
    role="${service}_app"
    database="${service}_db"

    actual=$(psql -X -h 127.0.0.1 -U "$role" -d "$database" -Atc "SELECT current_user || '|' || current_database()" --set ON_ERROR_STOP=1)
    [[ "$actual" == "$role|$database" ]] || { echo "FAIL owner identity for $service"; exit 1; }
    owner=$(psql -X -h 127.0.0.1 -U "$role" -d "$database" -Atc "SELECT pg_get_userbyid(datdba) = current_user FROM pg_database WHERE datname = current_database()" --set ON_ERROR_STOP=1)
    [[ "$owner" == "t" ]] || { echo "FAIL database ownership for $service"; exit 1; }
    privileges=$(psql -X -h 127.0.0.1 -U "$role" -d "$database" -Atc "SELECT NOT (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls) FROM pg_roles WHERE rolname = current_user" --set ON_ERROR_STOP=1)
    [[ "$privileges" == "t" ]] || { echo "FAIL excessive role privileges for $service"; exit 1; }
    schema_access=$(psql -X -h 127.0.0.1 -U "$role" -d "$database" -Atc "SELECT has_schema_privilege(current_user, 'public', 'USAGE') AND has_schema_privilege(current_user, 'public', 'CREATE')" --set ON_ERROR_STOP=1)
    [[ "$schema_access" == "t" ]] || { echo "FAIL future migration permissions for $service"; exit 1; }
    echo "PASS $role authenticates to $database and owns usable schema permissions"

    if denied=$(PGPASSWORD="intentionally-wrong-$RANDOM-$RANDOM" psql -X -h 127.0.0.1 -U "$role" -d "$database" -Atc 'SELECT 1' 2>&1); then
        echo "FAIL wrong password accepted for $role"; exit 1
    fi
    [[ "$denied" == *"password authentication failed"* ]] || { echo "FAIL expected authentication denial for $role"; exit 1; }
    echo "PASS $role rejects incorrect password"

    for target in identity catalog inventory ordering; do
        [[ "$target" == "$service" ]] && continue
        if denied=$(psql -X -h 127.0.0.1 -U "$role" -d "${target}_db" -Atc 'SELECT 1' 2>&1); then
            echo "FAIL $role accessed ${target}_db"; exit 1
        fi
        [[ "$denied" == *"permission denied for database"* ]] || { echo "FAIL expected CONNECT denial for $role -> ${target}_db"; exit 1; }
        echo "PASS $role denied CONNECT to ${target}_db"
    done
done

echo "PASS all 4 own-database logins, 4 wrong-password denials and 12 cross-service CONNECT denials"
