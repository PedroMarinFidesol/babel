namespace Babel.Domain.Enums;

public enum FileExtensionType
{
    Unknown = 0,
    TextBased = 1,      // .txt, .md, .json, .xml
    ImageBased = 2,     // .jpg, .png, .tiff, .bmp
    Pdf = 3,
    OfficeDocument = 4  // .docx, .xlsx
}
