#!/bin/bash
# =============================================================================
# ThreadForge - Configuration SSL avec Let's Encrypt
# =============================================================================
# Ce script configure HTTPS avec des certificats Let's Encrypt
# Prérequis: Un nom de domaine pointant vers l'IP du VPS
# =============================================================================

set -e

# Couleurs pour l'affichage
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Détection de Docker Compose (plugin vs standalone)
if command -v $DOCKER_COMPOSE &> /dev/null; then
    DOCKER_COMPOSE="$DOCKER_COMPOSE"
elif docker compose version &> /dev/null 2>&1; then
    DOCKER_COMPOSE="docker compose"
else
    echo -e "${RED}✗ Docker Compose non trouvé${NC}"
    exit 1
fi

# Répertoire du script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_DIR"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  ThreadForge - Configuration SSL${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# -----------------------------------------------------------------------------
# Vérification des variables
# -----------------------------------------------------------------------------
if [ ! -f .env ]; then
    echo -e "${RED}✗ Fichier .env introuvable${NC}"
    exit 1
fi

source .env

if [ -z "$DOMAIN" ]; then
    echo -e "${RED}✗ Variable DOMAIN non définie dans .env${NC}"
    echo ""
    echo "Ajoutez ces lignes dans votre fichier .env:"
    echo "  DOMAIN=votre-domaine.com"
    echo "  EMAIL=votre-email@exemple.com"
    exit 1
fi

if [ -z "$EMAIL" ]; then
    echo -e "${RED}✗ Variable EMAIL non définie dans .env${NC}"
    echo "Ajoutez EMAIL=votre-email@exemple.com dans .env"
    exit 1
fi

echo -e "${GREEN}Domaine: ${DOMAIN}${NC}"
echo -e "${GREEN}Email: ${EMAIL}${NC}"
echo ""

# -----------------------------------------------------------------------------
# Vérification DNS
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[1/5] Vérification de la configuration DNS...${NC}"
PUBLIC_IP=$(curl -sf https://ipinfo.io/ip)
DOMAIN_IP=$(dig +short "$DOMAIN" 2>/dev/null | head -1)

if [ "$PUBLIC_IP" != "$DOMAIN_IP" ]; then
    echo -e "${RED}⚠ Le domaine $DOMAIN ne pointe pas vers ce serveur${NC}"
    echo "  IP du serveur: $PUBLIC_IP"
    echo "  IP du domaine: $DOMAIN_IP"
    echo ""
    echo "Configurez un enregistrement A dans votre DNS:"
    echo "  $DOMAIN -> $PUBLIC_IP"
    echo ""
    read -p "Continuer quand même ? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
else
    echo -e "${GREEN}✓ DNS correctement configuré ($DOMAIN -> $PUBLIC_IP)${NC}"
fi

# -----------------------------------------------------------------------------
# Installation de Certbot
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[2/5] Installation de Certbot...${NC}"
if ! command -v certbot &> /dev/null; then
    sudo apt-get update
    sudo apt-get install -y certbot
    echo -e "${GREEN}✓ Certbot installé${NC}"
else
    echo -e "${GREEN}✓ Certbot déjà installé${NC}"
fi

# -----------------------------------------------------------------------------
# Arrêt de Nginx pour libérer le port 80
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[3/5] Préparation pour la génération du certificat...${NC}"
$DOCKER_COMPOSE -f $DOCKER_COMPOSE.prod.yml stop nginx 2>/dev/null || true
echo -e "${GREEN}✓ Port 80 libéré${NC}"

# -----------------------------------------------------------------------------
# Génération du certificat
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[4/5] Génération du certificat SSL...${NC}"
sudo certbot certonly \
    --standalone \
    --preferred-challenges http \
    --agree-tos \
    --email "$EMAIL" \
    -d "$DOMAIN" \
    --non-interactive

# Copier les certificats dans le répertoire du projet
echo "Copie des certificats..."
sudo mkdir -p certbot/conf/live/"$DOMAIN"
sudo cp -L /etc/letsencrypt/live/"$DOMAIN"/fullchain.pem certbot/conf/live/"$DOMAIN"/
sudo cp -L /etc/letsencrypt/live/"$DOMAIN"/privkey.pem certbot/conf/live/"$DOMAIN"/
sudo chmod -R 755 certbot/conf

echo -e "${GREEN}✓ Certificat SSL généré${NC}"

# -----------------------------------------------------------------------------
# Mise à jour de la configuration Nginx
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[5/5] Mise à jour de la configuration Nginx...${NC}"

# Créer une nouvelle configuration Nginx avec HTTPS
cat > nginx/nginx.conf << 'NGINX_EOF'
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
    use epoll;
    multi_accept on;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for"';

    access_log /var/log/nginx/access.log main;

    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;

    gzip on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_min_length 1000;
    gzip_types
        text/plain
        text/css
        text/xml
        text/javascript
        application/json
        application/javascript
        application/xml
        application/xml+rss
        application/x-javascript
        image/svg+xml;

    limit_req_zone $binary_remote_addr zone=general:10m rate=10r/s;

    upstream app {
        server app:8080;
        keepalive 32;
    }

    # HTTP -> HTTPS redirect
    server {
        listen 80;
        server_name DOMAIN_PLACEHOLDER;
        return 301 https://$server_name$request_uri;
    }

    # HTTPS Server
    server {
        listen 443 ssl http2;
        server_name DOMAIN_PLACEHOLDER;

        # SSL certificates
        ssl_certificate /etc/letsencrypt/live/DOMAIN_PLACEHOLDER/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/DOMAIN_PLACEHOLDER/privkey.pem;

        # SSL configuration
        ssl_session_timeout 1d;
        ssl_session_cache shared:SSL:50m;
        ssl_session_tickets off;

        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;

        # HSTS
        add_header Strict-Transport-Security "max-age=63072000" always;

        # Security headers
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header Referrer-Policy "strict-origin-when-cross-origin" always;

        client_max_body_size 10M;

        location /api/v1/health {
            proxy_pass http://app;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        location /api/ {
            limit_req zone=general burst=20 nodelay;
            proxy_pass http://app;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_set_header Connection "";
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }

        location /gateway {
            proxy_pass http://app;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        location / {
            proxy_pass http://app;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_set_header Connection "";

            location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
                proxy_pass http://app;
                proxy_http_version 1.1;
                proxy_set_header Host $host;
                expires 1y;
                add_header Cache-Control "public, immutable";
            }
        }
    }
}
NGINX_EOF

# Remplacer le placeholder par le vrai domaine
sed -i "s/DOMAIN_PLACEHOLDER/$DOMAIN/g" nginx/nginx.conf

echo -e "${GREEN}✓ Configuration Nginx mise à jour pour HTTPS${NC}"

# -----------------------------------------------------------------------------
# Mise à jour de .env pour CORS
# -----------------------------------------------------------------------------
echo ""
echo "Mise à jour de CORS_ORIGIN dans .env..."
sed -i "s|CORS_ORIGIN=.*|CORS_ORIGIN=https://$DOMAIN|" .env

# Mise à jour JWT Issuer/Audience
sed -i "s|JWT_ISSUER=.*|JWT_ISSUER=https://$DOMAIN|" .env
sed -i "s|JWT_AUDIENCE=.*|JWT_AUDIENCE=https://$DOMAIN|" .env

echo -e "${GREEN}✓ Variables d'environnement mises à jour${NC}"

# -----------------------------------------------------------------------------
# Redémarrage des services
# -----------------------------------------------------------------------------
echo ""
echo "Redémarrage des services..."
$DOCKER_COMPOSE -f $DOCKER_COMPOSE.prod.yml up -d

# -----------------------------------------------------------------------------
# Configuration du renouvellement automatique
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}Configuration du renouvellement automatique...${NC}"

# Créer le script de renouvellement
cat > scripts/renew-ssl.sh << 'RENEW_EOF'
#!/bin/bash
# Renouvellement automatique des certificats SSL

PROJECT_DIR="$(dirname "$(dirname "$(readlink -f "$0")")")"
cd "$PROJECT_DIR"

# Renouveler le certificat
certbot renew --quiet

# Copier les nouveaux certificats
source .env
if [ -n "$DOMAIN" ]; then
    cp -L /etc/letsencrypt/live/"$DOMAIN"/fullchain.pem certbot/conf/live/"$DOMAIN"/
    cp -L /etc/letsencrypt/live/"$DOMAIN"/privkey.pem certbot/conf/live/"$DOMAIN"/

    # Recharger Nginx
    $DOCKER_COMPOSE -f $DOCKER_COMPOSE.prod.yml exec -T nginx nginx -s reload
fi
RENEW_EOF

chmod +x scripts/renew-ssl.sh

# Ajouter la tâche cron
(crontab -l 2>/dev/null | grep -v "renew-ssl.sh"; echo "0 3 * * * $PROJECT_DIR/scripts/renew-ssl.sh >> /var/log/certbot-renew.log 2>&1") | crontab -

echo -e "${GREEN}✓ Renouvellement automatique configuré (tous les jours à 3h)${NC}"

# -----------------------------------------------------------------------------
# Résumé
# -----------------------------------------------------------------------------
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  SSL configuré avec succès !${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Votre application est maintenant accessible en HTTPS:"
echo -e "  ${BLUE}https://$DOMAIN${NC}"
echo ""
echo "Le certificat sera renouvelé automatiquement."
echo ""
