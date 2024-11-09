import pandas as pd
import numpy as np
from datetime import datetime, timedelta

# Parameters for data generation
n = 500  # number of samples

# Generate columns
start_date = datetime.now() - timedelta(hours=n)
dates = [start_date + timedelta(hours=i) for i in range(n)]
room_numbers = np.random.randint(1, 11, size=n)  # Room numbers between 1 and 10
fenetre_ouverte = np.random.choice([0, 1], size=n)
temp_ext = np.random.uniform(5, 35, size=n)
humidite = np.random.uniform(30, 80, size=n)

# Define rooms with and without people-count sensors
rooms_with_sensors = [2, 3, 4, 5, 6, 7, 8,9]  # Rooms with people-count sensors (1-10 people)
rooms_without_sensors = [room for room in range(1, 11) if room not in rooms_with_sensors]

# Generate 'nb_personnes' based on room type
nb_personnes = [
    np.random.randint(1, 11) if room in rooms_with_sensors else 0
    for room in room_numbers
]

# Generate 'temp_int' with influence of people count and other factors
temp_int = (
    20 + (fenetre_ouverte * (temp_ext - 20)) * 0.5 + np.array(nb_personnes) * 0.3 + np.random.normal(0, 1.5, size=n)
)

# Create DataFrame and save to CSV
data = pd.DataFrame({
    'date': dates,
    'room_number': room_numbers,
    'fenetre_ouverte': fenetre_ouverte,
    'temp_ext': temp_ext,
    'nb_personnes': nb_personnes,
    'humidite': humidite,
    'temp_int': temp_int
})

data.to_csv('C:\\Users\\romwe\\Documents\\My Web Sites\\SAE5\\IA\\TemperaturePrediction\\donnees_temperature.csv', index=False)
print("CSV file with room-specific person counting generated successfully.")
