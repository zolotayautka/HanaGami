using System;
using Microsoft.Data.Sqlite;

enum Resultcode
{
    success,
    connect_fail,
    revision_conflict,
    not_found,
    unknown_error
}

class hanagami_api
{
    private static readonly string? dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".mdnotes");
    private string api_base_url = "";
    private string auth_token = "";
    private readonly string get_list = "/api/notes/list/";
    private readonly string get_note = "/api/notes/get/";
    private readonly string post_note = "/api/notes/post";
    private readonly string post_delete = "/api/notes/delete/";
    private readonly string post_modify = "/api/notes/modify/";

    public hanagami_api(string base_url)
    {
        var sql = "SELECT api_base_url, auth_token FROM settings";
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            api_base_url = reader.GetString(0);
            auth_token = reader.GetString(1);
        }
    }

    public void setup(string base_url, string token)
    {
        
    }

    public Resultcode connected()
    {
        return Resultcode.success; // Placeholder implementation
    }

    public Resultcode add_note(Note new_note)
    {
        return Resultcode.success; // Placeholder implementation
    }   

    public Resultcode delete_note(int global_id)
    {
        return Resultcode.success; // Placeholder implementation
    }

    public Resultcode modify_note(Note modified_note)
    {
        return Resultcode.success; // Placeholder implementation
    }
}