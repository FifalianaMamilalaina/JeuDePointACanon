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
* **Taille de Grille** : La grille est stockée dans un tableau 2D `int[width + 1, height + 1]` car nous jouons sur les intersections.
* **Algorithme de Victoire** : Utilise une recherche bidirectionnelle dans 4 axes (Horizontal, Vertical, 2 Diagonales).
* **Validation de Partage** : `SharesMoreThanOnePointWithAnyExistingLine` itère sur `allLines` pour garantir qu'un nouveau coup ne crée pas une ligne trop proche d'une ancienne.
* **Intersection de Diagonales** : `DoesLineCrossOpponent` détecte si un segment diagonal traverse un segment adverse en vérifiant les "croisements en X".

### 2. Interface de Jeu (`GameForm.cs`)
Gère le rendu graphique et les événements.
* **Rendu GDI+** : Tout le plateau est dessiné dans l'événement `GridPanel_Paint`. 
    * `SmoothingMode.AntiAlias` est activé pour des points lisses.
    * Les points sont dessinés via `FillEllipse` centrés sur les coordonnées d'intersection.
* **Animation de la Balle** : Utilise un `System.Windows.Forms.Timer` (30ms). À chaque tick, la position de la balle est mise à jour (`ballX`, `ballY`) et une détection de collision est faite en comparant la distance aux intersections.
* **Gestion du Focus** : `KeyPreview = true` permet à la fenêtre de capturer les flèches directionnelles et les raccourcis Ctrl même si un bouton a le focus.

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
* Le facteur de vitesse est défini par `float speed = 5;`.
* La distance max (`ballMaxDist`) suit la règle de trois basée sur `shotPower`.

---

## ⚠️ Notes de Développement
* **Ambiguïté de Timer** : Toujours utiliser le namespace complet `System.Windows.Forms.Timer` pour éviter les conflits avec `System.Threading.Timer`.
* **Calcul des Coordonnées** : 
    * `gx = (e.X - cannonMargin) / cellSize` : Conversion Pixel -> Grille.
    * `px = cannonMargin + gx * cellSize` : Conversion Grille -> Pixel.
* **Nullable Types** : Le projet a été compilé sous .NET 9.0 avec les "Nullable checks" activés. Attention aux warnings CS8618 lors des modifications de modèles.
