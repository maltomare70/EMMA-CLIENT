using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmmaClientAv.Helpers;

public static class DateHelper
{
    /// <summary>
    /// Converte da yyyy-MM-dd a dd/MM/yyyy
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string ConvertData(string data)
    {

        string dataDoc;
        if (DateTime.TryParseExact(data, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime dataConvertita))
            dataDoc = dataConvertita.ToString("dd/MM/yyyy");
        else
            dataDoc = data;

        return dataDoc;
    }
}
