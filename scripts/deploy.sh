#!/bin/bash
# =============================================================================
# ThreadForge - Script de déploiement
# =============================================================================
# Ce script build et démarre l'application en production
# =============================================================================

set -e

# Couleurs pour l'affichage
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Répertoire du script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_DIR"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  ThreadForge - Déploiement${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# -----------------------------------------------------------------------------
# Vérification du fichier .env
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[1/5] Vérification de la configuration...${NC}"
if [ ! -f .env ]; then
    echo -e "${RED}✗ Fichier .env introuvable${NC}"
    echo ""
    echo "Créez le fichier .env à partir du template:"
    echo "  cp .env.example .env"
    echo "  nano .env"
    exit 1
fi

# Vérifier les variables obligatoires
source .env

if [ -z "$POSTGRES_PASSWORD" ] || [ "$POSTGRES_PASSWORD" = "CHANGEZ_MOI_mot_de_passe_securise_32_caracteres" ]; then
    echo -e "${RED}✗ POSTGRES_PASSWORD non configuré dans .env${NC}"
    exit 1
fi

if [ -z "$JWT_SECRET" ] || [ "$JWT_SECRET" = "CHANGEZ_MOI_secret_jwt_minimum_32_caracteres_aleatoires" ]; then
    echo -e "${RED}✗ JWT_SECRET non configuré dans .env${NC}"
    exit 1
fi

if [ -z "$GATEWAY_PASSWORD" ] || [ "$GATEWAY_PASSWORD" = "CHANGEZ_MOI_mot_de_passe_gateway" ]; then
    echo -e "${RED}✗ GATEWAY_PASSWORD non configuré dans .env${NC}"
    exit 1
fi

if [ -z "$XAI_API_KEY" ] || [ "$XAI_API_KEY" = "xai-VOTRE_CLE_API_ICI" ]; then
    echo -e "${RED}✗ XAI_API_KEY non configuré dans .env${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Configuration validée${NC}"

# -----------------------------------------------------------------------------
# Arrêt des conteneurs existants
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[2/5] Arrêt des conteneurs existants...${NC}"
docker compose -f docker-compose.prod.yml down --remove-orphans 2>/dev/null || true
echo -e "${GREEN}✓ Conteneurs arrêtés${NC}"

# -----------------------------------------------------------------------------
# Build des images
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[3/5] Construction des images Docker...${NC}"
echo "Cela peut prendre plusieurs minutes lors du premier build..."
docker compose -f docker-compose.prod.yml build --no-cache
echo -e "${GREEN}✓ Images construites${NC}"

# -----------------------------------------------------------------------------
# Démarrage des services
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[4/5] Démarrage des services...${NC}"
docker compose -f docker-compose.prod.yml up -d
echo -e "${GREEN}✓ Services démarrés${NC}"

# -----------------------------------------------------------------------------
# Vérification de santé
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}[5/5] Vérification de l'état des services...${NC}"
echo "Attente du démarrage (30 secondes)..."
sleep 30

# Vérifier l'état des conteneurs
echo ""
echo "État des conteneurs:"
docker compose -f docker-compose.prod.yml ps

# Vérifier le health check
echo ""
echo "Test du endpoint de santé..."
if curl -sf http://localhost/api/v1/health > /dev/null 2>&1; then
    echo -e "${GREEN}✓ API accessible et fonctionnelle${NC}"
else
    echo -e "${YELLOW}⚠ L'API n'est pas encore prête, vérifiez les logs:${NC}"
    echo "  docker compose -f docker-compose.prod.yml logs app"
fi

# -----------------------------------------------------------------------------
# Résumé
# -----------------------------------------------------------------------------
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Déploiement terminé !${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Récupérer l'IP publique si possible
PUBLIC_IP=$(curl -sf https://ipinfo.io/ip 2>/dev/null || echo "VOTRE_IP")

echo "Votre application est accessible à:"
echo -e "  ${BLUE}http://${PUBLIC_IP}${NC}"
echo ""
echo "Protection Gateway activée - mot de passe requis pour accéder."
echo ""
echo "Commandes utiles:"
echo "  Voir les logs:        docker compose -f docker-compose.prod.yml logs -f"
echo "  Logs de l'app:        docker compose -f docker-compose.prod.yml logs -f app"
echo "  Redémarrer:           docker compose -f docker-compose.prod.yml restart"
echo "  Arrêter:              docker compose -f docker-compose.prod.yml down"
echo ""
