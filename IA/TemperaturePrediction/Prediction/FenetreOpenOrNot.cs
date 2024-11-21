using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML.Trainers.FastTree;

public class TemperatureDataFenetre
{
    [LoadColumn(0)]
    public DateTime Date { get; set; }

    [LoadColumn(1)]
    public float RoomNumber { get; set; }

    [LoadColumn(5)]
    public float TempInt { get; set; }

     [LoadColumn(6)]
    public float Co2 { get; set; }
}

public class FenetreOuverte
{
    [ColumnName("PredictedLabel")]
    public bool FenetreOuverteBool { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}

public class LabeledTemperatureDataFenetre
{
    public float TempInt { get; set; }
    public float RoomNumber { get; set; }
    public float Co2 {get;set;}

    [ColumnName("Label")]
    public bool Label { get; set; }  // Changed property name to match column name
}

public class TemperatureDataFenetreMap : ClassMap<TemperatureDataFenetre>
{
    public TemperatureDataFenetreMap()
    {
        Map(m => m.Date).Name("date");
        Map(m => m.RoomNumber).Name("room_number");
        Map(m => m.TempInt).Name("temp_int");
        Map(m => m.Co2).Name("co2");
    }
}

public class FenetreBoolOuverte
{
    private readonly string _dataPath;
    private readonly MLContext _context;
    private ITransformer? _model;
    private PredictionEngine<TemperatureDataFenetre, FenetreOuverte>? _predictionEngine;

    // Adjusted thresholds for better detection
    private const float TEMPERATURE_THRESHOLD = 20.0f;  // Increased from 18.0f
    private const float SIGNIFICANT_TEMP_DROP = -0.8f;  // Less strict than -1.5f
    private const float TEMP_DROP_TIME_WINDOW = 10;     // Minutes to consider for temperature drop
    private const float SIGNIFICANT_CO2_DROP = -50.0f; 
    private const float CO2_THRESHOLD = 450.0f;  
    public FenetreBoolOuverte(string dataPath)
    {
        _dataPath = dataPath;
        _context = new MLContext(seed: 42);
    }


    public void TrainModel()
    {
        if (!File.Exists(_dataPath))
        {
            throw new FileNotFoundException($"File not found: {_dataPath}");
        }

        var records = ReadTemperatureData(_dataPath);
        var labeledData = GenerateLabeledData(records);

        // Print class distribution before training
        var windowOpenCount = labeledData.Count(x => x.Label);
        var windowClosedCount = labeledData.Count(x => !x.Label);
        Console.WriteLine($"Initial class distribution:");
        Console.WriteLine($"Windows Open: {windowOpenCount} ({(float)windowOpenCount / labeledData.Count:P2})");
        Console.WriteLine($"Windows Closed: {windowClosedCount} ({(float)windowClosedCount / labeledData.Count:P2})");

        var trainingData = _context.Data.LoadFromEnumerable(labeledData);

        var pipeline = _context.Transforms.Concatenate("Features",
    nameof(LabeledTemperatureDataFenetre.RoomNumber), // Make sure this is numeric
    nameof(LabeledTemperatureDataFenetre.TempInt) ,
    nameof(LabeledTemperatureDataFenetre.Co2)   // Make sure this is numeric
)
.Append(_context.Transforms.NormalizeMinMax("Features"))
.Append(_context.BinaryClassification.Trainers.FastTree(
    new FastTreeBinaryTrainer.Options
    {
        NumberOfTrees = 100,
        NumberOfLeaves = 20,
        MinimumExampleCountPerLeaf = 5,
        LabelColumnName = "Label"
    }));


        Console.WriteLine("\nTraining model...");
        _model = pipeline.Fit(trainingData);
        _predictionEngine = _context.Model.CreatePredictionEngine<TemperatureDataFenetre, FenetreOuverte>(_model);

        // Evaluate model
        var predictions = _model.Transform(trainingData);
        var metrics = _context.BinaryClassification.Evaluate(predictions);

        Console.WriteLine($"\nModel Metrics:");
        Console.WriteLine($"Accuracy: {metrics.Accuracy:F2}");
        Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:F2}");
        Console.WriteLine($"F1 Score: {metrics.F1Score:F2}");
        Console.WriteLine($"Positive Precision: {metrics.PositivePrecision:F2}");
        Console.WriteLine($"Negative Recall: {metrics.NegativeRecall:F2}");
    }

    private List<LabeledTemperatureDataFenetre> GenerateLabeledData(List<TemperatureDataFenetre> records)
    {
        var labeledData = new List<LabeledTemperatureDataFenetre>();

        foreach (var room in records.Select(r => r.RoomNumber).Distinct())
        {
            var roomData = records.Where(r => r.RoomNumber == room)
                                .OrderBy(r => r.Date)
                                .ToList();

            for (int i = 1; i < roomData.Count; i++)
            {
                // Calculate temperature changes over different time periods
                var shortTermTempDiff = roomData[i].TempInt - roomData[i - 1].TempInt;
                var shortTermCo2Diff = roomData[i].Co2 - roomData[i-1].Co2;
                // Look back up to TEMP_DROP_TIME_WINDOW minutes for significant drops
                var timeWindow = roomData[i].Date.AddMinutes(-TEMP_DROP_TIME_WINDOW);
                var previousTemps = roomData.Where(r => r.Date >= timeWindow && r.Date < roomData[i].Date)
                                          .OrderBy(r => r.Date)
                                          .ToList();

                var isWindowOpen = IsWindowLikelyOpen(
                    currentTemp: roomData[i].TempInt,
                    shortTermTempDiff: shortTermTempDiff,
                    currentCo2: roomData[i].Co2,
                    shortTermCo2Diff: shortTermCo2Diff,
                    previousTemps: previousTemps
                );

                labeledData.Add(new LabeledTemperatureDataFenetre
                {
                    TempInt = roomData[i].TempInt,
                    RoomNumber = room,
                    Co2 = roomData[i].Co2,
                    Label = isWindowOpen
                });
            }
        }

        return labeledData;
    }

    private bool IsWindowLikelyOpen(float currentTemp, float shortTermTempDiff, float currentCo2, float shortTermCo2Diff, List<TemperatureDataFenetre> previousTemps)
{
    // Vérification : Chute rapide et significative de la température
    if (shortTermTempDiff < SIGNIFICANT_TEMP_DROP)
        return true;

    // Vérification : Chute rapide et significative du CO₂
    if (shortTermCo2Diff < SIGNIFICANT_CO2_DROP)
        return true;

    // Vérification : Température intérieure basse
    if (currentTemp < TEMPERATURE_THRESHOLD - 2.0f)
        return true;

    // Vérification : Taux de CO₂ bas
    if (currentCo2 < CO2_THRESHOLD)
        return true;

    // Vérification : Chute graduelle de température sur la fenêtre d'analyse
    if (previousTemps.Any())
    {
        var maxTempInWindow = previousTemps.Max(t => t.TempInt);
        var totalDrop = currentTemp - maxTempInWindow;
        if (totalDrop < SIGNIFICANT_TEMP_DROP * 2) // Plus tolérant pour les baisses progressives
            return true;

        // Vérification : Chute graduelle de CO₂ sur la fenêtre d'analyse
        var maxCo2InWindow = previousTemps.Max(t => t.Co2);
        var totalCo2Drop = currentCo2 - maxCo2InWindow;
        if (totalCo2Drop < SIGNIFICANT_CO2_DROP * 2)
            return true;
    }

    // Vérification : Température en dessous du seuil avec une baisse récente
    if (currentTemp < TEMPERATURE_THRESHOLD && shortTermTempDiff < 0)
        return true;

    // Vérification : CO₂ en dessous du seuil avec une baisse récente
    if (currentCo2 < CO2_THRESHOLD && shortTermCo2Diff < 0)
        return true;

    return false;
}


    public bool PredictIfWindowIsOpenOverTime(int roomNumber, DateTime startTime, int intervalMinutes, float initialTempInt, string dataPathCSV)
    {
        if (_predictionEngine == null)
        {
            throw new InvalidOperationException("Model has not been trained. Call TrainModel() first.");
        }

        var records = ReadTemperatureData(dataPathCSV);
        var roomData = records.Where(r => r.RoomNumber == roomNumber)
                             .OrderBy(r => r.Date)
                             .ToList();

        if (!roomData.Any())
        {
            Console.WriteLine($"No data found for room {roomNumber}");
            return false;
        }

        var windowOpenPeriods = new List<(DateTime Start, DateTime End)>();
        var currentTime = startTime;
        float lastTemp = initialTempInt;
        bool isCurrentlyOpen = false;
        DateTime? windowOpenStart = null;

        // Keep track of consecutive predictions for more stable results
        const int CONSECUTIVE_PREDICTIONS_NEEDED = 3;
        var recentPredictions = new Queue<bool>();

        while (currentTime < startTime.AddMinutes(intervalMinutes))
        {
            var nearestRecord = roomData
                .Where(r => r.Date <= currentTime)
                .OrderByDescending(r => r.Date)
                .FirstOrDefault();

            if (nearestRecord != null)
            {
                var prediction = _predictionEngine.Predict(nearestRecord);
                var tempDiff = nearestRecord.TempInt - lastTemp;

                // Add prediction to recent predictions queue
                recentPredictions.Enqueue(prediction.FenetreOuverteBool);
                if (recentPredictions.Count > CONSECUTIVE_PREDICTIONS_NEEDED)
                    recentPredictions.Dequeue();

                // Only change state if we have consistent predictions
                var consistentPrediction = recentPredictions.Count == CONSECUTIVE_PREDICTIONS_NEEDED &&
                                         recentPredictions.All(p => p == recentPredictions.First());

                if (consistentPrediction)
                {
                    var windowShouldBeOpen = recentPredictions.First();

                    if (windowShouldBeOpen && !isCurrentlyOpen)
                    {
                        isCurrentlyOpen = true;
                        windowOpenStart = currentTime;
                    }
                    else if (!windowShouldBeOpen && isCurrentlyOpen)
                    {
                        isCurrentlyOpen = false;
                        if (windowOpenStart.HasValue)
                        {
                            windowOpenPeriods.Add((windowOpenStart.Value, currentTime));
                        }
                    }
                }

                lastTemp = nearestRecord.TempInt;
            }

            currentTime = currentTime.AddMinutes(1);
        }

        if (isCurrentlyOpen && windowOpenStart.HasValue)
        {
            windowOpenPeriods.Add((windowOpenStart.Value, currentTime));
        }

        if (windowOpenPeriods.Any())
        {
            foreach (var period in windowOpenPeriods)
            {
                var duration = period.End - period.Start;
            }
            return true;
        }

        return false;
    }

    public List<TemperatureDataFenetre> ReadTemperatureData(string csvFilePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            HeaderValidated = null,
        };

        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TemperatureDataFenetreMap>();
        return csv.GetRecords<TemperatureDataFenetre>().ToList();
    }

    public static List<float> GetUniqueRooms(string dataPath)
    {
        var fenetreModel = new FenetreBoolOuverte(dataPath);
        var records = fenetreModel.ReadTemperatureData(dataPath);
        return records.Select(r => r.RoomNumber).Distinct().OrderBy(r => r).ToList();
    }
}