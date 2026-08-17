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
    // 2026-08-14T09:12:33Z | Nikita 5124 | ANUN
    public class DuplicateNameLogger : IDuplicateNameLogger
    {
        private const string Separator = " | ";

        private static readonly SemaphoreSlim FileLock = new SemaphoreSlim(1, 1);

        public string FilePath { get; }

        public DuplicateNameLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            FilePath = filePath;
        }

        public async Task AppendAsync(IEnumerable<string> names, string source)
        {
            var list = names?.Where(n => !string.IsNullOrWhiteSpace(n))
                             .Select(n => n.Trim())
                             .ToList() ?? new List<string>();

            if (list.Count == 0)
                return;

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string cleanSource = string.IsNullOrWhiteSpace(source) ? "-" : source.Trim();

            var lines = list.Select(name => string.Join(Separator, timestamp, cleanSource, name));

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

                if (parts.Length >= 3)
                {
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
