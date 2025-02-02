using DavideBotHelper.Services.Extensions;
using OfficeOpenXml;

namespace DavideBotHelper.Services;

public class ExcelMovimentiService
{
    private readonly ILogger<ExcelMovimentiService> _log;
    private readonly string _excelFolderPath;

    private const string FilenamePart = "Movimenti ";
    private const string FileExtension = ".xlsx";
    private const string TemplateFilename = "Movimenti_template.xlsx";

    #region ExcelFileConstants

    private const int DataColumnNumber = 2;
    private const int SpesaColumnNumber = 3;
    private const int DescrizioneSpesaColumnNumber = 4;
    private const int EntrataColumnNumber = 5;
    private const int DescrizioneEntrataColumnNumber = 5;
    
    // private const uint GennaioSheetNumber = 1;
    // private const uint FebbraioSheetNumber = 2;
    // private const uint MarzoSheetNumber = 3;
    // private const uint AprileSheetNumber = 4;
    // private const uint MaggioSheetNumber = 5;
    // private const uint GiugnoSheetNumber = 6;
    // private const uint LuglioSheetNumber = 7;
    // private const uint AgostoSheetNumber = 8;
    // private const uint SettembreSheetNumber = 9;
    // private const uint OttobreSheetNumber = 10;
    // private const uint NovembreSheetNumber = 11;
    // private const uint DicembreSheetNumber = 12;

    #endregion
    


    public ExcelMovimentiService(ILogger<ExcelMovimentiService> log, IConfiguration configuration)
    {
        _log = log;
        _excelFolderPath = configuration.GetRequiredSection("BotConfig").GetRequiredSection("DirectoryPath").Value!;
    }

    public async Task<bool> AddMovimentoSpesa(decimal valore, string descrizione, int? anno, int? mese, int? giorno)
    {
        try
        {
            if (valore <= 0)
            {
                _log.Error("Valore {valore} non valido", valore);
                return false;
            }

            if (string.IsNullOrEmpty(descrizione))
            {
                _log.Error("Descrizione {descrizione} non deve essere vuota", descrizione);
                return false;
            }
            
            var today = DateTime.Today;

            string filePath = FindOrCopyFile(anno, today); 
            
            FileInfo existingFile = new FileInfo(filePath);
            using ExcelPackage package = new ExcelPackage();
            await package.LoadAsync(existingFile);
            
            int excelSheet = mese ?? today.Month; 
            ExcelWorksheet worksheet = package.Workbook.Worksheets[excelSheet];
            
            int emptyCellRow = FindFirstEmptyCellRow(worksheet, SpesaColumnNumber);
            if (emptyCellRow == -1)
            {
                _log.Error("Non è stato possibile identificare una cella libera entro le prime 1000");
                return false;
            }
            
            worksheet.SetValue(emptyCellRow, SpesaColumnNumber, valore);
            worksheet.SetValue(emptyCellRow, DescrizioneSpesaColumnNumber, descrizione);

            if (anno.HasValue && mese.HasValue && giorno.HasValue)
            {
                DateTime data = new DateTime(anno.Value, mese.Value, giorno.Value);
                worksheet.SetValue(emptyCellRow, DataColumnNumber, data);
            }
            
            await package.SaveAsAsync(existingFile);
            
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Errore durante l'aggiunta del movimento di spesa");
            return false;
        }
        return true;
    }
    
    public async Task<bool> AddMovimentoEntrata(decimal valore, string descrizione, int? anno, int? mese, int? giorno)
    {
        try
        {
            if (valore <= 0)
            {
                _log.Error("Valore {valore} non valido", valore);
                return false;
            }

            if (string.IsNullOrEmpty(descrizione))
            {
                _log.Error("Descrizione {descrizione} non deve essere vuota", descrizione);
                return false;
            }
            
            var today = DateTime.Today;

            string filePath = FindOrCopyFile(anno, today); 
            
            FileInfo existingFile = new FileInfo(filePath);
            using ExcelPackage package = new ExcelPackage();
            await package.LoadAsync(existingFile);
            
            int excelSheet = mese ?? today.Month; 
            ExcelWorksheet worksheet = package.Workbook.Worksheets[excelSheet];
            
            int emptyCellRow = FindFirstEmptyCellRow(worksheet, EntrataColumnNumber);
            if (emptyCellRow == -1)
            {
                _log.Error("Non è stato possibile identificare una cella libera entro le prime 1000");
                return false;
            }
            
            worksheet.SetValue(emptyCellRow, EntrataColumnNumber, valore);
            worksheet.SetValue(emptyCellRow, DescrizioneEntrataColumnNumber, descrizione);

            if (anno.HasValue && mese.HasValue && giorno.HasValue)
            {
                DateTime data = new DateTime(anno.Value, mese.Value, giorno.Value);
                worksheet.SetValue(emptyCellRow, DataColumnNumber, data);
            }
            
            await package.SaveAsAsync(existingFile);
            
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Errore durante l'aggiunta del movimento di entrata");
            return false;
        }
        return true;
    }

    private string FindOrCopyFile(int? anno, DateTime today)
    {
        string annoFile = anno.HasValue ? anno.Value.ToString() : today.Year.ToString();
        string filename = $"{FilenamePart}{annoFile}{FileExtension}";
        string filePath = Path.Combine(_excelFolderPath, filename);

        if (!File.Exists(filePath))
        {
            File.Copy(Path.Combine(_excelFolderPath, TemplateFilename), filePath);
        }
        return filePath;
    }
    private int FindFirstEmptyCellRow(ExcelWorksheet excelWorksheet, int column)
    {
        //Limito la ricerca a 1000 righe circa. Parto dalla riga 3 dove troverò la prima cella valorizzabile
        for (var row = 3; row < 1000; row++)
        {
            var rowValue = excelWorksheet.Cells[row, column].Value;
            if (rowValue is null)
            {
                return row;
            }
        }
        return -1;
    }
}