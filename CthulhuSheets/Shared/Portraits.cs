namespace CthulhuSheets.Shared;

public static class Portraits
{
    // Maximum portrait file size. Used both for the upload picker (PortraitDialog)
    // and for re-importing an exported character (MainLayout). These must match:
    // a portrait accepted on upload can be exported, so import has to accept it back.
    public const long MaxBytes = 5 * 1024 * 1024;
}
