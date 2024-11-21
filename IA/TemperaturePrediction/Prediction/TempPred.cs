using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;

public class TemperatureData
{
    [LoadColumn(0)] // Date
    public DateTime Date { get; set; }

    [LoadColumn(1)] // Room number
    public int RoomNumber { get; set; }


    [LoadColumn(2)] // temp_ext
    public float TempExt { get; set; }

    [LoadColumn(3)] // nb_personnes
    public float NbPersonnes { get; set; }

    [LoadColumn(4)] // humidite
    public float Humidite { get; set; }

    [LoadColumn(5)] // temp_int
    public float TempInt { get; set; }
}

public class TemperaturePrediction
{
    [ColumnName("Score")]
    public float PredictedTempInt { get; set; }
}

public class TemperaturePredictionModel
{
    private readonly string _dataPath;
    private readonly MLContext _context;
    private PredictionEngine<TemperatureData, TemperaturePrediction> _predictionEngine;

    public TemperaturePredictionModel(string dataPath)
    {
        _dataPath = dataPath;
        _context = new MLContext();
    }

    public void TrainModel()
    {
        if (!File.Exists(_dataPath))
        {
            throw new FileNotFoundException("File not found: " + _dataPath);
        }

        // Load data
        var data = _context.Data.LoadFromTextFile<TemperatureData>(_dataPath, separatorChar: ',', hasHeader: true);
        var trainTestData = _context.Data.TrainTestSplit(data, testFraction: 0.2);
        var trainingData = trainTestData.TrainSet;

        // Build and train model
        var pipeline = _context.Transforms
            .Conversion.ConvertType("RoomNumberFloat", "RoomNumber", outputKind: DataKind.Single) // Convert RoomNumber to float
            .Append(_context.Transforms.Concatenate("Features", "RoomNumberFloat", "TempExt", "NbPersonnes", "Humidite"))
            .Append(_context.Regression.Trainers.Sdca(labelColumnName: "TempInt", maximumNumberOfIterations: 100));

        var model = pipeline.Fit(trainingData);

        // Create prediction engine
        _predictionEngine = _context.Model.CreatePredictionEngine<TemperatureData, TemperaturePrediction>(model);

        // Evaluate model
        var metrics = _context.Regression.Evaluate(model.Transform(trainTestData.TestSet), labelColumnName: "TempInt");
        Console.WriteLine($"Model trained. MAE: {metrics.MeanAbsoluteError}, MSE: {metrics.MeanSquaredError}");
    }

    public void PredictTemperatureForRoom(int room)
    {
        try
        {
            // Vérification si la salle existe dans les données
            var availableRooms = TemperaturePredictionModel.GetUniqueRooms(_dataPath);
            if (!availableRooms.Contains(room))
            {
                Console.WriteLine($"No data available for Room {room}.");
                return;
            }

            // Récupérer la dernière observation pour la salle
            var lastObservation = GetLastObservationByRoom(room);
            var roomPredictions = new Dictionary<string, float>();

            // Générer des prédictions pour les 17 prochaines heures
            for (int hour = 0; hour < 17; hour++)
            {
                var futureDate = DateTime.Now.AddHours(hour);
                var simulatedData = SimulateFutureData(lastObservation, new Random());

                var prediction = _predictionEngine.Predict(simulatedData).PredictedTempInt;
                roomPredictions[futureDate.ToString("dddd HH:mm")] = prediction;
            }

            // Afficher les prédictions
            Console.WriteLine($"Predictions for Room {room}:");
            foreach (var prediction in roomPredictions)
            {
                Console.WriteLine($"{prediction.Key}: {(int)prediction.Value}°C");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error predicting temperature for Room {room}: {ex.Message}");
        }
    }

    private TemperatureData SimulateFutureData(TemperatureData lastObservation, Random random)
    {
        // More realistic simulation with bounds checking
        return new TemperatureData
        {
            Date = DateTime.Now,
            RoomNumber = lastObservation.RoomNumber,
            // Keep temperature changes within reasonable bounds (-1 to +1 degree)
            TempExt = Math.Max(0, Math.Min(40, lastObservation.TempExt + (float)(random.NextDouble() * 2 - 1))),
            // People count should stay realistic
            NbPersonnes = Math.Max(0, Math.Min(30, lastObservation.NbPersonnes + (random.Next(-1, 2)))),
            // Humidity should stay between 0-100%
            Humidite = Math.Max(0, Math.Min(100, lastObservation.Humidite + (float)(random.NextDouble() * 10 - 5))),
            TempInt = lastObservation.TempInt // This will be predicted by the model
        };
    }
    public static List<int> GetUniqueRooms(string dataPath)
    {
        if (!File.Exists(dataPath))
            throw new FileNotFoundException("File not found: " + dataPath);

        // Initialisez la liste ou une autre structure de données pour stocker les résultats valides
        List<int> roomNumbers = new List<int>();

        foreach (var line in File.ReadLines(dataPath).Skip(1)) // Skip header row
        {
            var columns = line.Split(',');

            // Vérifiez si le nombre de colonnes est correct
            if (columns.Length < 6)
            {
                Console.WriteLine($"Data is malformed (insufficient columns): {line}");
                continue; // Passez à la ligne suivante si la ligne est malformée
            }

            // Essayez de parser chaque valeur attendue (par exemple, numéro de chambre et température)
            if (!int.TryParse(columns[1], out var room) || room < 0 ||
                !double.TryParse(columns[2], out var temperature) ||
                !double.TryParse(columns[5], out var predictedTemp))  // Vérifiez que les températures sont valides
            {
                Console.WriteLine($"Error predicting temperature for Room {columns[1]}: Data is malformed in the last observation. Line: {line}");
                continue; // Ignore cette ligne et passe à la suivante si les données sont malformées
            }

            // Ajoutez le numéro de chambre dans la liste si la ligne est valide
            roomNumbers.Add(room);

            // Traitement des autres données (par exemple, la prédiction de température)
            // Vous pouvez effectuer des opérations supplémentaires avec la température ici
            // par exemple, en ajoutant la température à une liste, ou en effectuant une prédiction
        }

        return roomNumbers.Distinct().ToList();
    }

    private TemperatureData GetLastObservationByRoom(int roomNumber)
    {
        var roomLines = File.ReadAllLines(_dataPath)
            .Skip(1) // Skip header
            .Where(line =>
            {
                var columns = line.Split(',');
                return columns.Length >= 6 && // Ensure we have all required columns
                       int.TryParse(columns[1], out int roomNum) &&
                       roomNum == roomNumber;
            })
            .ToArray();

        if (!roomLines.Any())
            throw new InvalidDataException($"No data found for room number {roomNumber}.");

        // Get the last line for debugging
        var lastLine = roomLines.Last().Split(',');
        Console.WriteLine($"Last line for room {roomNumber}: {string.Join(",", lastLine)}");

        // Ensure that the last line has the expected number of columns (6, not 7)
        if (lastLine.Length < 6)
        {
            throw new InvalidDataException($"Data is malformed in the last observation for room {roomNumber}. Expected 6 columns, got {lastLine.Length}");
        }

        // Parse the data with better error handling
        if (!DateTime.TryParseExact(lastLine[0], "yyyy-MM-dd HH:mm:ss.ffffff",
                                   CultureInfo.InvariantCulture,
                                   DateTimeStyles.None,
                                   out var date))
        {
            throw new InvalidDataException($"Invalid date format in line: {lastLine[0]}");
        }

        // Create TemperatureData with proper column mapping
        return new TemperatureData
        {
            Date = date,
            RoomNumber = int.Parse(lastLine[1]),
            TempExt = float.Parse(lastLine[2], CultureInfo.InvariantCulture),
            NbPersonnes = float.Parse(lastLine[3], CultureInfo.InvariantCulture),
            Humidite = float.Parse(lastLine[4], CultureInfo.InvariantCulture),
            TempInt = float.Parse(lastLine[5], CultureInfo.InvariantCulture)
        };
    }
}
