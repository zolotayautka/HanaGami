using GLib;

class hanagami_api
{
    private string api_base_url = "";

    public hanagami_api(string base_url)
    {
        api_base_url = base_url;
    }

    public bool connected()
    {
        return true;
    }
}