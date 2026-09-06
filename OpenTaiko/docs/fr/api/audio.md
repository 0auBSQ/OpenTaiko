<!-- api/audio.md -->

# Audio

Chargement et lecture des sons, et lecture des jeux de sons de frappe installés.

## SOUND

Crée des handles de son à partir de fichiers audio. Chaque méthode de fabrique affecte le son à un groupe de volume (effet sonore, voix, lecture de chanson ou aperçu de chanson), et le groupe détermine le réglage de volume utilisateur qui s'applique.

<div class="callout warn">
Les chemins relatifs sont résolus par rapport au répertoire du script ; les variantes FromAbsolutePath prennent un chemin complet. Le handle renvoyé commence vide pendant que le thread de rendu construit le flux au cours des frames suivantes ; le handle met en file les appels Play et Set* effectués avant cela et les applique une fois le flux prêt. Le jeu libère les handles quand il décharge le script ; appelez Dispose pour en libérer un plus tôt.
</div>

| Méthode | Description |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | Charge un effet sonore. |
| `SOUND:CreateVoice(path)  -> sound` | Charge un extrait vocal. |
| `SOUND:CreateBGM(path)  -> sound` | Charge une musique de fond (groupe de lecture de chanson). |
| `SOUND:CreatePreview(path)  -> sound` | Charge un extrait d'aperçu de chanson (groupe d'aperçu de chanson). |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | Charge un effet sonore depuis un chemin complet. |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | Charge un extrait vocal depuis un chemin complet. |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | Charge une musique de fond depuis un chemin complet. |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | Charge un extrait d'aperçu de chanson depuis un chemin complet. |

```lua
local bgm, decide

function onStart()
    bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    bgm:SetLoop(true)
    decide = SOUND:CreateSFX("Sounds/Decide.ogg")
end

function activate()
    bgm:Play()
end

function update()
    if INPUT:Pressed("Decide") then decide:Play() end
end

function deactivate()
    bgm:Stop()
end

function onDestroy()
    bgm:Dispose()
    decide:Dispose()
end
```

## Handle de son

Un son lisible renvoyé par les méthodes de fabrique de `SOUND`.

<div class="callout warn">
Tant que le flux n'est pas prêt, le handle met en tampon Play et chaque appel Set*, et les accesseurs renvoient les valeurs par défaut listées ci-dessous. Le volume est un pourcentage (100 = le niveau propre du fichier). Le panoramique va de -100 (gauche) à 100 (droite) en passant par 0 (centre). Les positions et durées sont en millisecondes. Play redémarre le flux depuis le début ; positionnez-vous donc après avoir appelé Play.
</div>

| Méthode | Description |
| --- | --- |
| `sound:Play()  -> nil` | Démarre (ou redémarre) la lecture depuis le début. |
| `sound:Start()  -> nil` | Identique à Play. |
| `sound:Pause()  -> nil` | Met la lecture en pause. |
| `sound:Resume()  -> nil` | Reprend la lecture après Pause. |
| `sound:Stop()  -> nil` | Arrête la lecture. |
| `sound:Reset()  -> nil` | Revient au début. |
| `sound:SetLoop(loop)  -> nil` | Active ou désactive la lecture en boucle. |
| `sound:GetLoop()  -> bool` | Indique si la lecture en boucle est activée. |
| `sound:SetVolumePercent(vol)  -> nil` | Définit le volume en pourcentage. |
| `sound:GetVolumePercent()  -> number` | Renvoie le volume en pourcentage (100 quand non chargé). |
| `sound:SetVolume(vol)  -> nil` | Ancien nom de SetVolumePercent (entier). |
| `sound:SetPan(pan)  -> nil` | Définit le panoramique stéréo, -100..100. |
| `sound:GetPan()  -> int` | Renvoie le panoramique courant (0 quand non chargé). |
| `sound:SetSpeed(speed)  -> nil` | Définit le multiplicateur de vitesse de lecture (1 = normal). |
| `sound:GetSpeed()  -> number` | Renvoie le multiplicateur de vitesse de lecture (1 quand non chargé). |
| `sound:SetTimestampMs(ms)  -> nil` | Se positionne à une position en millisecondes. |
| `sound:SetTimestamp(ms)  -> nil` | Ancien nom de SetTimestampMs. |
| `sound:GetTimestampMs()  -> number` | Renvoie la position courante en millisecondes (0 quand non chargé). |
| `sound.DurationMs  -> number` | Durée totale en millisecondes (0 quand non chargé). |
| `sound:GetDurationMs()  -> number` | Ancien nom de DurationMs. |
| `sound.Loaded  -> bool` | Vrai une fois que le thread de rendu a construit le flux. |
| `sound.IsPlaying  -> bool` | Vrai pendant la lecture du son. |
| `sound:IsFinished()  -> bool` | Vrai une fois que la lecture a atteint la fin (faux en pause ou non chargé). |
| `sound.Path  -> string` | Le chemin complet du fichier chargé (vide tant que non chargé). |
| `sound:Dispose()  -> nil` | Libère le son. |

## HITSOUNDSLIST

Liste en lecture seule des jeux de sons de frappe installés dans le skin courant (le jeu les lit depuis `Global/HitSounds/`).

<div class="callout warn">
Disponible dans chaque script. Les indices commencent à 0. GetByName compare le nom de dossier du jeu sans tenir compte de la casse. Les deux recherches renvoient nil quand rien ne correspond.
</div>

| Méthode | Description |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | Nombre de jeux de sons de frappe. |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | Renvoie l'entrée à l'indice donné (à partir de 0), ou nil. |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | Renvoie l'entrée dont le nom de dossier correspond, ou nil. |

```lua
local don

function onStart()
    for i = 0, HITSOUNDSLIST.Count - 1 do
        local hs = HITSOUNDSLIST:GetByIndex(i)
        debugLog(hs.FolderName .. " -> " .. hs.DisplayName)
    end
    local taiko = HITSOUNDSLIST:GetByName("Taiko")
    if taiko then
        don = SOUND:CreateSFXFromAbsolutePath(taiko.DonPath)
    end
end

function onDestroy()
    if don then don:Dispose() end
end
```

## Entrée de sons de frappe

Un jeu de sons de frappe renvoyé par `HITSOUNDSLIST:GetByIndex` ou `GetByName`. Tous les membres sont en lecture seule.

| Méthode | Description |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | Nom de dossier du jeu (par exemple `"Taiko"`). |
| `hitsoundEntry.DisplayName  -> string` | Nom d'affichage localisé ; retombe sur le nom de dossier. |
| `hitsoundEntry.DonPath  -> string` | Chemin complet du fichier son Don du jeu. |
| `hitsoundEntry.KaPath  -> string` | Chemin complet du fichier son Ka du jeu. |
