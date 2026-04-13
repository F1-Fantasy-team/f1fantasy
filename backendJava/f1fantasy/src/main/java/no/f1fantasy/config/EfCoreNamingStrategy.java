package no.f1fantasy.config;

import org.hibernate.boot.model.naming.Identifier;
import org.hibernate.boot.model.naming.PhysicalNamingStrategy;
import org.hibernate.engine.jdbc.env.spi.JdbcEnvironment;

/**
 * Maps Java camelCase field names to PascalCase column names to match the
 * column naming convention used by .NET EF Core + Npgsql on the shared PostgreSQL schema.
 *
 * Examples:
 *   driverId   → DriverId
 *   season     → Season
 *   raceName   → RaceName
 *   isSprint   → IsSprint
 */
public class EfCoreNamingStrategy implements PhysicalNamingStrategy {

    @Override
    public Identifier toPhysicalCatalogName(Identifier name, JdbcEnvironment env) {
        return name;
    }

    @Override
    public Identifier toPhysicalSchemaName(Identifier name, JdbcEnvironment env) {
        return name;
    }

    @Override
    public Identifier toPhysicalTableName(Identifier name, JdbcEnvironment env) {
        // Table names are specified explicitly via @Table(name = "...") — pass through unchanged
        return name;
    }

    @Override
    public Identifier toPhysicalSequenceName(Identifier name, JdbcEnvironment env) {
        return name;
    }

    @Override
    public Identifier toPhysicalColumnName(Identifier name, JdbcEnvironment env) {
        if (name == null) return null;
        String text = name.getText();
        if (text == null || text.isEmpty()) return name;
        // Capitalise only the first character; leave the rest as-is
        String pascal = Character.toUpperCase(text.charAt(0)) + text.substring(1);
        return Identifier.toIdentifier(pascal, name.isQuoted());
    }
}
