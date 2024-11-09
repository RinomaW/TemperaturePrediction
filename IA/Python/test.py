import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.linear_model import LinearRegression
from sklearn.metrics import mean_absolute_error, mean_squared_error
from datetime import timedelta

# Charger les données
data = pd.read_csv('C:\\Users\\romwe\\Documents\\My Web Sites\\SAE5\\IA\\TemperaturePrediction\\donnees_temperature.csv')

# Supposons que les colonnes pertinentes soient 'fenetre_ouverte', 'temp_ext', 'nb_personnes', 'humidite'
X = data[['fenetre_ouverte', 'temp_ext', 'nb_personnes', 'humidite']]
y = data['temp_int']

# Division en ensembles d'entraînement et de test
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

# Initialisation et entraînement du modèle
model = LinearRegression()
model.fit(X_train, y_train)

# Prédiction sur l'ensemble de test
y_pred = model.predict(X_test)

# Évaluation du modèle
mae = mean_absolute_error(y_test, y_pred)
mse = mean_squared_error(y_test, y_pred)

print(f"Erreur absolue moyenne (MAE) : {mae}")
print(f"Erreur quadratique moyenne (MSE) : {mse}")

# Création de prédictions futures pour 2 heures, 1 jour, 2 jours, et 3 jours
intervals = [2, 24, 48, 72]  # heures à l'avance
predictions = {}

# Récupérer les dernières valeurs observées pour les simulations futures
last_observation = data.iloc[-1]

for hours in intervals:
    # Calcul de la date future pour information
    future_date = pd.to_datetime(last_observation['date']) + timedelta(hours=hours)
    
    # Simuler de nouvelles conditions pour chaque intervalle basé sur les dernières valeurs
    humidite_future = last_observation['humidite'] * (1 + np.random.uniform(-0.05, 0.05))  # variation aléatoire
    temp_ext_future = last_observation['temp_ext'] + np.random.uniform(-1, 1)  # variation légère de température extérieure
    nb_personnes_future = last_observation['nb_personnes']  # même nombre de personnes
    fenetre_ouverte_future = last_observation['fenetre_ouverte']  # même état de la fenêtre

    # Préparer les données pour la prédiction
    future_data = pd.DataFrame([[fenetre_ouverte_future, temp_ext_future, nb_personnes_future, humidite_future]],
                               columns=['fenetre_ouverte', 'temp_ext', 'nb_personnes', 'humidite'])
    
    # Prédiction
    prediction = model.predict(future_data)[0]
    predictions[f"{hours} heures (à {future_date})"] = prediction

# Affichage des prédictions
for interval, pred in predictions.items():
    print(f"Température prédite pour {interval} : {pred:.2f}°C")
