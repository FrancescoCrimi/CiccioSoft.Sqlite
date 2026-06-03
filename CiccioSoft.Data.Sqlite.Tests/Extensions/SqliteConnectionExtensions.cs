// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Data.Sqlite;

namespace CiccioSoft.Data.Sqlite;

internal static class SqliteConnectionExtensions
{
    public static SqliteDataReader ExecuteReader(
        this SqliteConnection connection,
        string commandText,
        params SqliteParameter[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddRange(parameters);

        return (SqliteDataReader)command.ExecuteReader();
    }




    public static int ExecuteNonQuery(
        this SqliteConnection connection,
        string commandText,
        params SqliteParameter[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddRange(parameters);

        return command.ExecuteNonQuery();
    }


    public static T ExecuteScalar<T>(
        this SqliteConnection connection,
        string commandText,
        params SqliteParameter[] parameters)
        => (T)connection.ExecuteScalar(commandText, parameters)!;

    private static object? ExecuteScalar(
        this SqliteConnection connection,
        string commandText,
        params SqliteParameter[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddRange(parameters);

        return command.ExecuteScalar();
    }
}
