# Guide de Déploiement - ThreadForge

Ce guide vous accompagne pas à pas pour déployer ThreadForge sur un VPS.

## Prérequis

- **VPS**: Ubuntu 22.04+ avec 2 Go RAM minimum
- **Docker**: Déjà installé sur le VPS
- **Clé API xAI**: Obtenue sur [console.x.ai](https://console.x.ai/)
- **Accès SSH**: Connexion root ou sudo au VPS

## Déploiement Rapide (5 étapes)

### 1. Connexion au VPS

```bash
ssh root@VOTRE_IP_VPS
```

### 2. Cloner le projet

```bash
cd /opt
git clone https://github.com/VOTRE_UTILISATEUR/threadforge.git
cd threadforge
```

### 3. Vérifier les prérequis

```bash
./scripts/vps-setup.sh
```

Ce script vérifie que Docker fonctionne et configure le pare-feu.

### 4. Configurer l'environnement

```bash
cp .env.example .env
nano .env
```

**Variables à modifier obligatoirement:**

| Variable | Description | Exemple |
|----------|-------------|---------|
| `POSTGRES_PASSWORD` | Mot de passe base de données | `MonMotDePasse123!Secure` |
| `JWT_SECRET` | Secret pour les tokens (min 32 car.) | `openssl rand -base64 32` |
| `GATEWAY_PASSWORD` | Mot de passe pour accéder à l'app | `MonAccesSecurise123` |
| `XAI_API_KEY` | Clé API xAI | `xai-xxxxxxxxxxxx` |

**Générer un JWT_SECRET sécurisé:**
```bash
openssl rand -base64 32
```

### 5. Lancer le déploiement

```bash
./scripts/deploy.sh
```

Le script va:
1. Valider la configuration
2. Construire les images Docker (peut prendre 5-10 min la première fois)
3. Démarrer les services
4. Vérifier que tout fonctionne

## Accès à l'Application

Une fois déployé, accédez à:
```
http://VOTRE_IP_VPS
```

Vous verrez une page de connexion demandant le **mot de passe Gateway** que vous avez configuré.

## Commandes Utiles

> **Note**: Utilisez `docker compose` (avec espace) ou `docker-compose` (avec tiret) selon votre installation. Sur Hostinger, c'est généralement `docker compose`.

```bash
# Voir l'état des services
docker compose -f docker-compose.prod.yml ps

# Voir les logs en temps réel
docker compose -f docker-compose.prod.yml logs -f

# Logs d'un service spécifique
docker compose -f docker-compose.prod.yml logs -f app
docker compose -f docker-compose.prod.yml logs -f nginx
docker compose -f docker-compose.prod.yml logs -f db

# Redémarrer les services
docker compose -f docker-compose.prod.yml restart

# Arrêter les services
docker compose -f docker-compose.prod.yml down

# Mettre à jour l'application
git pull
./scripts/deploy.sh
```

## Ajout HTTPS (avec un domaine)

Quand vous avez un nom de domaine:

### 1. Configurer le DNS

Ajoutez un enregistrement A dans votre gestionnaire DNS:
```
Type: A
Nom: @ (ou votre sous-domaine)
Valeur: VOTRE_IP_VPS
TTL: 3600
```

### 2. Mettre à jour .env

```bash
nano .env
```

Ajoutez ces lignes:
```bash
DOMAIN=votre-domaine.com
EMAIL=votre-email@exemple.com
```

### 3. Activer SSL

```bash
./scripts/setup-ssl.sh
```

Ce script va:
- Vérifier que le DNS est configuré
- Générer un certificat Let's Encrypt
- Configurer Nginx pour HTTPS
- Mettre en place le renouvellement automatique

Votre site sera alors accessible en HTTPS:
```
https://votre-domaine.com
```

## Dépannage

### L'application ne démarre pas

```bash
# Vérifier les logs de l'app
docker compose -f docker-compose.prod.yml logs app

# Problème courant: variables d'environnement manquantes
cat .env | grep -E "POSTGRES_PASSWORD|JWT_SECRET|XAI_API_KEY|GATEWAY_PASSWORD"
```

### Erreur de base de données

```bash
# Vérifier que PostgreSQL est démarré
docker compose -f docker-compose.prod.yml ps db

# Voir les logs de la DB
docker compose -f docker-compose.prod.yml logs db
```

### Port 80 déjà utilisé

```bash
# Voir quel processus utilise le port 80
sudo lsof -i :80

# Si c'est Apache, l'arrêter
sudo systemctl stop apache2
sudo systemctl disable apache2
```

### Reconstruire depuis zéro

```bash
# Arrêter et supprimer tout
docker compose -f docker-compose.prod.yml down -v

# Reconstruire
docker compose -f docker-compose.prod.yml build --no-cache

# Redémarrer
docker compose -f docker-compose.prod.yml up -d
```

## Sauvegarde

### Sauvegarder la base de données

```bash
# Créer une sauvegarde
docker compose -f docker-compose.prod.yml exec db pg_dump -U postgres threadforge > backup_$(date +%Y%m%d).sql

# Restaurer une sauvegarde
cat backup_20240115.sql | docker compose -f docker-compose.prod.yml exec -T db psql -U postgres threadforge
```

### Sauvegarder la configuration

```bash
# Copier le fichier .env (contient les secrets)
cp .env .env.backup
```

## Architecture

```
┌─────────────────────────────────────┐
│              Internet               │
└──────────────┬──────────────────────┘
               │ Port 80 (HTTP)
               │ Port 443 (HTTPS si configuré)
┌──────────────▼──────────────────────┐
│            Nginx                    │
│    (Reverse Proxy + SSL)            │
└──────────────┬──────────────────────┘
               │ Port 8080 (interne)
┌──────────────▼──────────────────────┐
│         .NET Application            │
│    (API + Frontend Angular)         │
└──────────────┬──────────────────────┘
               │ Port 5432 (interne)
┌──────────────▼──────────────────────┐
│          PostgreSQL                 │
│        (Base de données)            │
└─────────────────────────────────────┘
```

## Checklist de Sécurité

- [ ] Mot de passe PostgreSQL fort (16+ caractères)
- [ ] JWT_SECRET unique et aléatoire (32+ caractères)
- [ ] GATEWAY_PASSWORD configuré
- [ ] Pare-feu activé (ports 22, 80, 443 uniquement)
- [ ] SSH avec clé (désactiver l'authentification par mot de passe)
- [ ] HTTPS activé (quand domaine disponible)
- [ ] Sauvegardes régulières configurées

## Support

En cas de problème:
1. Vérifiez les logs: `docker compose -f docker-compose.prod.yml logs`
2. Consultez la section Dépannage ci-dessus
3. Ouvrez une issue sur le repository GitHub
