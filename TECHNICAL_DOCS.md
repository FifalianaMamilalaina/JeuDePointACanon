# Documentation Technique - Point Game C#

Cette documentation détaille l'implémentation technique du projet pour faciliter toute modification future du code.

## 🏗️ Architecture Globale
Le projet utilise une architecture de type **N-Tier** simplifiée :
1. **Couche Présentation (Forms)** : Gère l'affichage et les entrées utilisateur (WinForms).
2. **Couche Logique (Services)** : Contient les algorithmes de jeu et les règles métier.
3. **Couche Données (Models & Service DB)** : Gère la persistance avec PostgreSQL.

---

## 💻 Détails Techniques des Composants

### 1. Moteur de Jeu (`GameLogic.cs`)
C'est le fichier le plus critique pour les règles.
* **Taille de Grille** : La grille est stockée dans un tableau 2D `int[width, height]`. Contrairement à la version initiale, `GridWidth` et `GridHeight` représentent désormais le nombre exact d'intersections (lignes de points) et non le nombre de cases.
* **Algorithme de Victoire** : Lorsqu'un alignement de 5 points ou plus est détecté, le jeu extrait un segment d'**exactement 5 points** incluant la pièce posée. C'est ce segment qui est validé comme ligne gagnante.
* **Validation de Partage** : `SharesMoreThanOnePointWithAnyExistingLine` garantit qu'une nouvelle ligne ne partage pas plus d'un point avec une ligne existante.
* **Intersection de Diagonales** : `DoesLineCrossOpponent` empêche de croiser physiquement une ligne adverse.

### 2. Interface de Jeu (`GameForm.cs`)
Gère le rendu graphique et les événements.
* **Rendu GDI+** : Le plateau dessine `GridWidth` lignes verticales et `GridHeight` lignes horizontales.
* **Animation de la Balle** : En mode tir, le mouvement est **uniquement horizontal**. La collision est détectée en vérifiant la proximité avec les intersections de la ligne sur laquelle se trouve le canon.
* **Gestion du Tour après Tir** : La méthode `EndShot` vérifie si une cible a été touchée. Si oui, le turn n'est pas incrémenté, permettant au tireur de rejouer.

### 3. Persistance (`DatabaseService.cs`)
Utilise la bibliothèque `Npgsql`.
* **Transactions** : Les sauvegardes de coups se font via `SaveMovesBulk` pour optimiser les performances en évitant de multiples appels réseau.
* **Nettoyage** : La méthode `DeleteGame` supprime en cascade les coups associés (via contrainte de clé étrangère ou suppression manuelle).

---

## 🔧 Guide de Modification

### Modifier une Règle de Victoire
Rendez-vous dans `GameLogic.cs` -> `CheckWin`. C'est ici que vous pouvez changer le nombre de points requis (actuellement 5) ou ajouter des conditions de blocage.

### Modifier l'Apparence (Points/Canons)
Rendez-vous dans `GameForm.cs` -> `GridPanel_Paint`.
* Pour la taille des points, modifiez `pointRadius`.
* Pour les canons, modifiez la méthode `DrawCannon`.

### Changer la Base de Données
1. Modifiez la `connectionString` dans `DatabaseService.cs`.
2. Si vous ajoutez des colonnes (ex: nom du joueur), mettez à jour les modèles dans `/Models` et les requêtes SQL dans `DatabaseService.cs`.

### Ajuster la Puissance des Canons
La logique se trouve dans `GameForm.cs` -> `FireBall`.
* La distance max (`ballMaxDist`) suit la règle de trois basée sur `shotPower` (1-9) et la largeur réelle de la grille (`game.GridWidth`).
* Le résultat est arrondi à l'entier le plus proche via `MidpointRounding.AwayFromZero` pour garantir que la balle s'arrête pile sur une intersection.
* Une puissance de 9 permet de traverser toute la largeur configurée.

---

## ⚠️ Notes de Développement
* **Ambiguïté de Timer** : Toujours utiliser le namespace complet `System.Windows.Forms.Timer` pour éviter les conflits avec `System.Threading.Timer`.
* **Calcul des Coordonnées** : 
    * `gx = (e.X - cannonMargin) / cellSize` : Conversion Pixel -> Grille.
    * `px = cannonMargin + gx * cellSize` : Conversion Grille -> Pixel.
* **Nullable Types** : Le projet a été compilé sous .NET 9.0 avec les "Nullable checks" activés. Attention aux warnings CS8618 lors des modifications de modèles.
