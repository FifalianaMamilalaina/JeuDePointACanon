# Explications des Dernières Modifications (alea.md)

Ce document détaille les deux dernières fonctionnalités majeures ajoutées au **Jeu de Points à Canons**.

---

## 1. Mécanique de Récupération (Claimed Spots)

### Concept
L'objectif est de permettre à un joueur de "reprendre" un point qui lui a été volé/détruit par un tir de canon adverse.

### Fonctionnement Technique
1.  **Enregistrement de la perte** : Lorsqu'un joueur (ex: Bleu) détruit un point adverse (ex: Rouge) avec son canon, le système enregistre l'emplacement $(X, Y)$ comme étant "revendiqué" par Rouge.
2.  **Le Tir de Récupération** : Si Rouge tire plus tard avec son propre canon sur cet emplacement exact :
    *   Le système vérifie si Rouge possède une "revendication" sur ce point.
    *   Si oui, le point Rouge est instantanément **reposé** à cet endroit.
    *   Si Bleu avait entre-temps posé un nouveau point sur cet emplacement, le point de Bleu est détruit et remplacé par celui de Rouge.
3.  **Protection** : Si Bleu a réussi à inclure son nouveau point dans une **ligne de 5 validée**, le point devient indestructible et Rouge ne peut plus le récupérer.
4.  **Usage Unique** : Une fois qu'un point a été récupéré, la revendication est effacée.

### Fichiers Clés
*   `Models/ClaimedSpot.cs` : Structure de données pour la revendication.
*   `GameLogic.cs` -> `TryReclaim` : Logique de vérification et de remplacement.
*   `DatabaseService.cs` : Table `claimed_spots` pour sauvegarder ces données.

---

## 2. Validation Automatique lors de la Récupération

### Concept
Initialement, les lignes n'étaient vérifiées que lors du placement manuel d'un point sur la grille. Cette modification permet de détecter une victoire même si le point est posé par un tir de canon.

### Fonctionnement Technique
Lorsqu'un point revient sur la grille via la mécanique de **Récupération** (décrite ci-dessus) :
1.  Le système appelle immédiatement une nouvelle méthode `PlaceMove_CheckOnly`.
2.  Cette méthode analyse les alentours du point récupéré dans les 4 axes (horizontal, vertical, diagonales).
3.  Si le point complète un alignement d'**exactement 5 points**, la ligne est créée, colorée, et le score du joueur est augmenté de +1.

### Pourquoi c'est important ?
Cela évite que des joueurs récupèrent des points stratégiques sans que le score ne se mette à jour, garantissant que toute action (pose manuelle ou tir tactique) est comptabilisée pour la victoire.

### Fichiers Clés
*   `GameLogic.cs` -> `PlaceMove_CheckOnly` : Vérifie les lignes sans modifier la grille (puisque le point est déjà posé).
*   `GameForm.cs` -> `BallTimer_Tick` : Déclenche la vérification dès que la balle confirme une récupération.
