# Jeu de Points à Canons (C# WinForms)

Bienvenue dans le projet **Jeu de Points à Canons**. Il s'agit d'un jeu de stratégie au tour par tour inspiré du Gomoku (5 de suite), mais avec des mécaniques de combat inédites (canons et destruction de points).

## 🚀 Présentation du Projet
Ce jeu permet à deux joueurs de s'affronter sur une grille personnalisable. L'objectif est de marquer le plus de points possible en alignant des points, tout en utilisant des canons pour détruire les points adverses et saboter leurs stratégies.

## 📜 Règles du Jeu et Logique

### 1. Placement des Points
* **Intersections** : Contrairement aux jeux classiques dans les cases, les points sont placés sur les **croisements (intersections)** des lignes de la grille.
* **Tour par tour** : Les joueurs placent un point à tour de rôle.
* **Score** : Réussir un alignement de **5 points ou plus** rapporte 1 point au score du joueur.
* **Tour supplémentaire** : Si un joueur marque un point, il conserve la main et peut rejouer immédiatement.

### 2. Règles de Validation (Gomoku avancé)
* **Pas de recyclage excessif** : Une nouvelle ligne ne peut pas réutiliser plus d'**un seul point** d'une ligne déjà existante.
* **Interdiction de couper** : On ne peut pas tracer une ligne qui traverse physiquement une ligne adverse déjà validée.
* *Localisation :* Cette logique est centralisée dans `GameLogic.cs` (méthodes `CheckWin`, `SharesMoreThanOnePointWithAnyExistingLine` et `DoesLineCrossOpponent`).

### 3. Mécanique des Canons (Le twist !)
Chaque joueur possède un canon situé sur le côté (Gauche pour P1, Droite pour P2).
* **Déplacement** : Le canon peut être déplacé verticalement avec les **flèches Haut/Bas** ou par un **clic direct** dans la zone du canon.
* **Mode Tir** : Le joueur peut basculer en mode tir via le bouton "Switch to Shoot".
* **Puissance (Règle de 3)** : La puissance (Ctrl + 1 à 9) définit la distance maximale de la balle. Si la grille fait 15 cases de long, une puissance de 9 traverse les 15 cases, une puissance de 1 traverse ~1.6 cases.
* **Destruction** : 
    * La balle détruit les points adverses qu'elle touche.
    * **Pas de tir allié** : Un joueur ne peut pas détruire ses propres points.
    * **Protection** : Les points faisant partie d'une ligne déjà validée sont indestructibles.
* *Localisation :* La gestion du canon et de l'animation de la balle est dans `GameForm.cs`. La validation de destruction est dans `GameLogic.RemovePoint`.

## 📂 Structure du Projet
* **`/Forms`** : Contient l'interface utilisateur.
    * `MainMenuForm.cs` : Écran d'accueil.
    * `GameSetupForm.cs` : Configuration (taille grille, couleurs).
    * `GameForm.cs` : Le cœur du jeu (Rendu GDI+, Animation canon).
    * `LoadGameForm.cs` : Gestion de la sauvegarde/chargement/suppression.
* **`/Models`** : Définition des données.
    * `Game.cs` : Objet représentant une partie.
    * `Move.cs` : Objet représentant un coup joué.
* **`/Services`** : Logique métier.
    * `GameLogic.cs` : Moteur de règles (victoire, collisions, validation).
    * `DatabaseService.cs` : Communication avec PostgreSQL (Npgsql).
* **`/bdd`** : Script SQL de création de la base de données.

## 🛠️ Installation et Lancement
1. Exécutez le script `db_setup.sql` dans votre instance PostgreSQL.
2. Configurez la chaîne de connexion dans `DatabaseService.cs`.
3. Lancez le projet avec `dotnet run`.
