# Jeu de Points à Canons (C# WinForms)

Bienvenue dans le projet **Jeu de Points à Canons**. Il s'agit d'un jeu de stratégie au tour par tour inspiré du Gomoku (5 de suite), mais avec des mécaniques de combat inédites (canons et destruction de points).

## 🚀 Présentation du Projet
Ce jeu permet à deux joueurs de s'affronter sur une grille personnalisable. L'objectif est de marquer le plus de points possible en alignant des points, tout en utilisant des canons pour détruire les points adverses et saboter leurs stratégies.

## 📜 Règles du Jeu et Logique

### 1. Placement des Points
* **Intersections** : Les points sont placés sur les **croisements (intersections)** des lignes de la grille. La taille configurée (ex: 9x9) définit exactement le nombre de lignes horizontales et verticales.
* **Tour par tour** : Les joueurs placent un point à tour de rôle.
* **Score** : Réussir un alignement d'**exactement 5 points** rapporte 1 point. Si vous alignez plus de 5 points, seul un segment de 5 est validé.
* **Tour supplémentaire** : Si un joueur marque un point, il conserve la main et peut rejouer immédiatement.

### 2. Règles de Validation (Gomoku avancé)
* **Pas de recyclage excessif** : Une nouvelle ligne ne peut pas réutiliser plus d'**un seul point** d'une ligne déjà existante.
* **Interdiction de couper** : On ne peut pas tracer une ligne qui traverse physiquement une ligne adverse déjà validée.
* *Localisation :* Cette logique est centralisée dans `GameLogic.cs` (méthodes `CheckWin`, `SharesMoreThanOnePointWithAnyExistingLine` et `DoesLineCrossOpponent`).

### 3. Mécanique des Canons
Chaque joueur possède un canon situé sur le côté (Gauche pour P1, Droite pour P2).
* **Déplacement** : Le canon se déplace verticalement avec les **flèches Haut/Bas** ou par **clic direct** dans la zone du canon (marges).
* **Mode Tir** : Le joueur bascule en mode tir via le bouton "Switch to Shoot". La ligne sur laquelle se trouve le canon est alors surlignée.
* **Tir Horizontal** : On ne vise plus manuellement. La balle part **strictement à l'horizontale** depuis la position du canon.
* **Puissance (Règle de 3)** : La puissance (Ctrl + 1 à 9) définit la distance horizontale. **Puissance 1 = première intersection, Puissance 9 = dernière intersection**, quelle que soit la taille de la grille.
* **Effets du Tir** : 
    * La balle ne détruit que le point situé **exactement à la distance choisie** (pas en chemin).
    * **Récupération du tour** : Si vous touchez un point adverse, vous **rejouez immédiatement**. Sinon, le tour passe à l'adversaire.
    * **Pas de tir allié** : Vos propres points sont immunisés.
    * **Protection** : Les points dans une ligne validée sont indestructibles.
* *Localisation :* Gestion dans `GameForm.cs` (FireBall / BallTimer_Tick) et `GameLogic.RemovePoint`.

### 4. Mécanique de Récupération (Claimed Spots)
* Quand un de vos points est **détruit** par un tir adverse, l'emplacement est automatiquement **revendiqué** par vous.
* Si vous tirez ensuite sur **cet emplacement exact**, votre point **revient** — même si un point adverse y a été posé entre-temps (il sera alors détruit et remplacé).
* **Exception** : Si le point adverse à cet emplacement fait partie d'une **ligne validée**, la récupération échoue.
* Les revendications sont **persistées** en base de données (table `claimed_spots`) et survivent aux sauvegardes/chargements.
* **Validation de ligne** : Si un point récupéré complète un alignement de 5, la ligne est **automatiquement validée** et le score mis à jour.
* *Localisation :* `GameLogic.TryReclaim`, `GameLogic.PlaceMove_CheckOnly`, `DatabaseService.SaveClaimedSpots/GetClaimedSpots`.

## 📂 Structure du Projet
* **`/Forms`** : Contient l'interface utilisateur.
    * `MainMenuForm.cs` : Écran d'accueil.
    * `GameSetupForm.cs` : Configuration (taille grille, couleurs).
    * `GameForm.cs` : Le cœur du jeu (Rendu GDI+, Animation canon).
    * `LoadGameForm.cs` : Gestion de la sauvegarde/chargement/suppression.
* **`/Models`** : Définition des données.
    * `Game.cs` : Objet représentant une partie.
    * `Move.cs` : Objet représentant un coup joué.
    * `ClaimedSpot.cs` : Emplacement revendiqué après destruction.
* **`/Services`** : Logique métier.
    * `GameLogic.cs` : Moteur de règles (victoire, collisions, validation).
    * `DatabaseService.cs` : Communication avec PostgreSQL (Npgsql).
* **`/bdd`** : Script SQL de création de la base de données.

## 🛠️ Installation et Lancement
1. Exécutez le script `db_setup.sql` dans votre instance PostgreSQL.
2. Configurez la chaîne de connexion dans `DatabaseService.cs`.
3. Lancez le projet avec `dotnet run`.
