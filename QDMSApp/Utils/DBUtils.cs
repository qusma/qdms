﻿// -----------------------------------------------------------------------
// <copyright file="DBUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MySql.Data.EntityFramework;
using MySql.Data.MySqlClient;
using QDMSApp.Properties;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Reflection;

namespace QDMSApp
{
    public static class DBUtils
    {
        public static void SetConnectionString()
        {
            if (Settings.Default.databaseType == "MySql")
            {
                SetMySqlConnectionString("qdmsEntities", "qdms");
                SetMySqlConnectionString("qdmsDataEntities", "qdmsdata");
            }
            else
            {
                SetSqlServerConnectionString("qdmsEntities", "qdms");
                SetSqlServerConnectionString("qdmsDataEntities", "qdmsdata");
            }

            ConfigurationManager.RefreshSection("connectionStrings");
        }

        public static void SetDbConfiguration()
        {
            if (Settings.Default.databaseType == "MySql")
            {
                DbConfiguration.SetConfiguration(new MySqlEFConfiguration());
            }
        }

        private static void SetSqlServerConnectionString(string stringName, string dbName)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings[stringName];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = GetSqlServerConnectionString(
                dbName,
                Settings.Default.sqlServerHost,
                Settings.Default.sqlServerUsername,
                EncryptionUtils.Unprotect(Settings.Default.sqlServerPassword),
                useWindowsAuthentication: Settings.Default.sqlServerUseWindowsAuthentication);
            conSettings.ProviderName = "System.Data.SqlClient";

            config.Save();
        }

        private static void SetMySqlConnectionString(string stringName, string dbName)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings[stringName];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = string.Format("User Id={0};Password={1};Host={2};Database={3};Persist Security Info=True;UseCompression=True;SslMode=none",
                Settings.Default.mySqlUsername,
                EncryptionUtils.Unprotect(Settings.Default.mySqlPassword),
                Settings.Default.mySqlHost,
                dbName);
            conSettings.ProviderName = "MySql.Data.MySqlClient";

            config.Save();
        }

        public static SqlConnection CreateSqlServerConnection(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = GetSqlServerConnectionString(database, server, username, password, noDB, useWindowsAuthentication);
            return new SqlConnection(connectionString);
        }

        internal static string GetSqlServerConnectionString(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = String.Format(
                "Data Source={0};",
                server ?? Settings.Default.sqlServerHost);

            if (!noDB)
            {
                connectionString += String.Format("Initial Catalog={0};", database);
            }

            if (!useWindowsAuthentication) //user/pass authentication
            {
                if (password == null)
                {
                    try
                    {
                        password = EncryptionUtils.Unprotect(Settings.Default.sqlServerPassword);
                    }
                    catch
                    {
                        password = "";
                    }
                }
                connectionString += string.Format("User ID={0};Password={1};", username, password);
            }
            else //windows authentication
            {
                connectionString += "Integrated Security=True;";
            }
            return connectionString;
        }

        public static MySqlConnection CreateMySqlConnection(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = GetMySqlServerConnectionString(database, server, username, password, noDB);
            return new MySqlConnection(connectionString);
        }

        internal static string GetMySqlServerConnectionString(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false)
        {
            if (password == null)
            {
                try
                {
                    password = EncryptionUtils.Unprotect(Settings.Default.mySqlPassword);
                }
                catch
                {
                    password = "";
                }
            }

            string connectionString = String.Format(
                "server={0};" +
                "user id={1};" +
                "Password={2};",
                server ?? Settings.Default.mySqlHost,
                username ?? Settings.Default.mySqlUsername,
                password
                );

            if (!noDB)
            {
                connectionString += String.Format("database={0};", database);
            }

            connectionString +=
                "allow user variables=true;" +
                "persist security info=true;" +
                "Convert Zero Datetime=True";

            return connectionString;
        }

        public static bool TestConnection()
        {
            if (Settings.Default.databaseType == "MySql")
            {
                //try to establish a database connection. If not possible, prompt the user to enter details
                var connection = DBUtils.CreateMySqlConnection(noDB: true);
                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }
                connection.Close();
            }
            else //SQL Server
            {
                //try to establish a database connection. If not possible, prompt the user to enter details
                var connection = DBUtils.CreateSqlServerConnection(noDB: true, useWindowsAuthentication: Settings.Default.sqlServerUseWindowsAuthentication);
                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }
                connection.Close();
            }

            return true;
        }
    }
}
