using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CPAWeb.Services.DTOs;
using CPAWeb.Services.Interface;

namespace CPAWeb.Business.Services.Services
{
    // Կրկնվող անունները պահում ենք պարզ .txt ֆայլում՝
    // 2026-08-14T09:12:33Z | Nikita 5124 | ANUN | 374088006492 | 2582 | Provider
    // (timestamp | source | name | service_name | service_id | provider)
    //
    // Հին՝ 3 դաշտանոց տողերը (առանց գրանցման տեղի) նույնպես կարդացվում են.
    public class DuplicateNameLogger : IDuplicateNameLogger
    {
        private const string Separator = " | ";
        private const int FieldCount = 6;

        private static readonly SemaphoreSlim FileLock = new SemaphoreSlim(1, 1);

        public string FilePath { get; }

        public DuplicateNameLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            FilePath = filePath;
        }

        // Դաշտի մեջ եղած "|"-ը կկոտրեր Split-ը, ուստի փոխարինում ենք
        private static string Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim().Replace("|", "/");

        public async Task AppendAsync(IEnumerable<DuplicateNameDto> duplicates, string source)
        {
            var list = duplicates?.Where(d => d != null && !string.IsNullOrWhiteSpace(d.Name))
                                  .ToList() ?? new List<DuplicateNameDto>();

            if (list.Count == 0)
                return;

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string cleanSource = Clean(source);

            var lines = list.Select(d => string.Join(
                Separator,
                timestamp,
                cleanSource,
                Clean(d.Name),
                Clean(d.ServiceName),
                Clean(d.ServiceId),
                Clean(d.ProviderName)));

            await FileLock.WaitAsync();
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.AppendAllLinesAsync(FilePath, lines);
            }
            finally
            {
                FileLock.Release();
            }
        }

        // Գրելիս դատարկ դաշտը դարձել է "-", կարդալիս հետ ենք բերում
        private static string Restore(string value)
            => value == "-" ? string.Empty : value;

        public async Task<List<DuplicateNameDto>> ReadAllAsync()
        {
            var result = new List<DuplicateNameDto>();

            if (!File.Exists(FilePath))
                return result;

            string[] lines;

            await FileLock.WaitAsync();
            try
            {
                lines = await File.ReadAllLinesAsync(FilePath);
            }
            finally
            {
                FileLock.Release();
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(Separator);

                if (parts.Length >= FieldCount)
                {
                    DateTime.TryParse(parts[0], out var detectedAt);

                    result.Add(new DuplicateNameDto
                    {
                        DetectedAt = detectedAt,
                        Source = parts[1],
                        Name = parts[2],
                        ServiceName = Restore(parts[3]),
                        ServiceId = Restore(parts[4]),
                        ProviderName = Restore(parts[5])
                    });
                }
                else if (parts.Length >= 3)
                {
                    // Հին ձևաչափ՝ առանց գրանցման տեղի
                    DateTime.TryParse(parts[0], out var detectedAt);

                    result.Add(new DuplicateNameDto
                    {
                        DetectedAt = detectedAt,
                        Source = parts[1],
                        // Անվան մեջ էլ կարող է " | " լինել, ուստի մնացածը միացնում ենք
                        Name = string.Join(Separator, parts.Skip(2))
                    });
                }
                else
                {
                    result.Add(new DuplicateNameDto { Name = line });
                }
            }

            // Ամենավերջինները վերևում
            result.Reverse();
            return result;
        }

        public async Task ClearAsync()
        {
            await FileLock.WaitAsync();
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            finally
            {
                FileLock.Release();
            }
        }
    }
}
