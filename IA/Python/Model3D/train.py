from ursina import *
import numpy as np
import random
import time  # Pour la gestion du temps

# Créer l'application Ursina
app = Ursina()

# Créer un sol
ground = Entity(model='plane', scale=(50, 1, 50), texture='grass', collider='box')

# Créer une caméra
camera.position = (40, 40, 40)
camera.look_at((0, 0, 0))

# Créer un objet (cube blanc) et un cube vert (objectif)
cube = Entity(model='cube', color=color.white, position=(-24, 1, 0), scale=(2, 2, 2))
cubeVictory = Entity(model='cube', color=color.green, position=(3, 3, 0), scale=(2, 2, 2))

# Paramètres d'apprentissage
alpha = 0.1  # Taux d'apprentissage
gamma = 0.9  # Facteur de discount
epsilon = 0.1  # Exploration vs exploitation
actions = [Vec3(1, 0, 0), Vec3(-1, 0, 0), Vec3(0, 0, 1), Vec3(0, 0, -1), Vec3(0, 1, 0), Vec3(0, -1, 0), Vec3(0, 3, 0)]  # Ajout du saut (déplacement vertical)

# Créer un tableau Q (états et actions)
q_table = np.zeros((100, 100, 100, len(actions)))  # 100x100x100 est une taille de "grille" arbitraire pour l'état

# Position initiale du cube
initial_position = Vec3(-24, 1, 0)

# Chronomètre pour gérer le temps écoulé
start_time = time.time()

def state_to_index(state):
    """Convertir l'état (position du cube) en indices de la table Q."""
    return (int(state.x + 50), int(state.y + 50), int(state.z + 50))  # Assurez-vous que l'état est dans la plage correcte

def choose_action(state):
    """Choisir une action en fonction de la stratégie epsilon-greedy."""
    if random.uniform(0, 1) < epsilon:
        return random.choice(actions)  # Exploration
    else:
        state_index = state_to_index(state)
        return actions[np.argmax(q_table[state_index])]  # Exploitation

def reward_function(agent_pos, goal_pos):
    """Calculer la récompense en fonction de la distance au but."""
    if cube.y < 0:
        # Réinitialiser la position du cube à son état initial
        cube.position = initial_position
        return -10  # Pénalité sévère pour être tombé

    distance = (agent_pos - goal_pos).length()
    if distance < 1:  # Si l'agent est très proche de l'objectif
        return 10  # Récompense élevée pour avoir atteint l'objectif
    else:
        return -distance  # Pénalité basée sur la distance restante

def display_victory_message():
    """Affiche un message de victoire lorsque l'agent atteint l'objectif."""
    victory_text = Text(
        text="Vous avez gagné !", 
        position=(0, 0), 
        scale=2, 
        origin=(0, 0), 
        color=color.green
    )
    # Afficher le message pendant 2 secondes
    invoke(victory_text.delete, delay=2)

def update():
    """Mettre à jour la position de l'agent et appliquer l'apprentissage par renforcement."""
    global cube, cubeVictory, q_table, alpha, gamma, initial_position, start_time

    # Empêcher le cube de changer de hauteur
    cube.y = max(1, cube.y)  # Fixe la hauteur du cube (ne laisse pas le cube descendre sous le sol)

    # Vérifier si le cube tombe sous le sol (y < 0)
    if cube.y < 0:
        # Réinitialiser la position du cube à son état initial
        cube.position = initial_position

    # Vérifier si 3 secondes se sont écoulées
    elapsed_time = time.time() - start_time
    if elapsed_time > 3:
        # Réinitialiser la position du cube à son état initial
        cube.position = initial_position
        start_time = time.time()  # Réinitialiser le chronomètre

    # Déterminer la position actuelle de l'agent (cube blanc)
    agent_pos = cube.position
    goal_pos = cubeVictory.position

    # Vérifier si le cube a atteint l'objectif
    distance_to_goal = (agent_pos - goal_pos).length()
    if distance_to_goal < 1:  # Si l'agent est très proche de l'objectif
        display_victory_message()  # Afficher le message de victoire
        cube.position = initial_position  # Réinitialiser la position du cube après la victoire
        return  # Arrêter l'update après la victoire

    # Choisir une action basée sur l'état actuel
    action = choose_action(agent_pos)

    # Appliquer l'action choisie
    if action == Vec3(0, 3, 0):  # Si l'action est un saut
        cube.y += 3  # Ajouter une élévation
    else:
        cube.position += action  # Déplacer selon les autres actions

    next_state = cube.position

    # Calculer la récompense
    reward = reward_function(next_state, goal_pos)

    # Mettre à jour la table Q avec l'apprentissage
    current_state_index = state_to_index(agent_pos)
    next_state_index = state_to_index(next_state)
    best_next_action_value = np.max(q_table[next_state_index])  # Meilleure valeur possible pour l'état suivant
    q_table[current_state_index][actions.index(action)] += alpha * (reward + gamma * best_next_action_value - q_table[current_state_index][actions.index(action)])

    # Afficher la position du cube et la récompense pour le debug
    print(f"Position: {cube.position}, Reward: {reward}, Time: {elapsed_time}")

# Lancer l'application
app.run()
