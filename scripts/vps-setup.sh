#!/bin/bash
# =============================================================================
# ThreadForge - Script de vérification et configuration VPS
# =============================================================================
# Ce script vérifie les prérequis et configure le pare-feu
# Docker étant déjà installé sur votre VPS Hostinger
# =============================================================================

set -e

# Couleurs pour l'affichage
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  ThreadForge - Vérification VPS${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# -----------------------------------------------------------------------------
# Vérification des prérequis
# -----------------------------------------------------------------------------

echo -e "${YELLOW}[1/4] Vérification de Docker...${NC}"
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version)
    echo -e "${GREEN}✓ Docker installé: ${DOCKER_VERSION}${NC}"
else
    echo -e "${RED}✗ Docker n'est pas installé${NC}"
    echo "Veuillez installer Docker avant de continuer."
    exit 1
fi

echo ""
echo -e "${YELLOW}[2/4] Vérification de Docker Compose...${NC}"
if command -v docker-compose &> /dev/null; then
    COMPOSE_VERSION=$(docker-compose --version)
    echo -e "${GREEN}✓ Docker Compose installé: ${COMPOSE_VERSION}${NC}"
elif docker compose version &> /dev/null; then
    COMPOSE_VERSION=$(docker compose version)
    echo -e "${GREEN}✓ Docker Compose (plugin) installé: ${COMPOSE_VERSION}${NC}"
else
    echo -e "${RED}✗ Docker Compose n'est pas installé${NC}"
    echo "Installation de Docker Compose..."
    sudo apt-get update
    sudo apt-get install -y docker-compose-plugin
    echo -e "${GREEN}✓ Docker Compose installé${NC}"
fi

echo ""
echo -e "${YELLOW}[3/4] Vérification du service Docker...${NC}"
if systemctl is-active --quiet docker; then
    echo -e "${GREEN}✓ Service Docker actif${NC}"
else
    echo -e "${YELLOW}⚠ Service Docker inactif, démarrage...${NC}"
    sudo systemctl start docker
    sudo systemctl enable docker
    echo -e "${GREEN}✓ Service Docker démarré${NC}"
fi

echo ""
echo -e "${YELLOW}[4/4] Configuration du pare-feu (UFW)...${NC}"
if command -v ufw &> /dev/null; then
    # Vérifier si UFW est actif
    if sudo ufw status | grep -q "Status: active"; then
        echo "UFW est actif, ajout des règles..."
        sudo ufw allow 80/tcp comment 'HTTP'
        sudo ufw allow 443/tcp comment 'HTTPS'
        sudo ufw allow 22/tcp comment 'SSH'
        echo -e "${GREEN}✓ Pare-feu configuré (ports 22, 80, 443 ouverts)${NC}"
    else
        echo -e "${YELLOW}⚠ UFW n'est pas actif${NC}"
        echo "Voulez-vous activer UFW avec les ports 22, 80, 443 ? (y/n)"
        read -r response
        if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
            sudo ufw default deny incoming
            sudo ufw default allow outgoing
            sudo ufw allow 22/tcp comment 'SSH'
            sudo ufw allow 80/tcp comment 'HTTP'
            sudo ufw allow 443/tcp comment 'HTTPS'
            sudo ufw --force enable
            echo -e "${GREEN}✓ Pare-feu activé et configuré${NC}"
        else
            echo -e "${YELLOW}⚠ Pare-feu non configuré - assurez-vous que les ports sont ouverts${NC}"
        fi
    fi
else
    echo -e "${YELLOW}⚠ UFW non installé - vérifiez manuellement que les ports 80 et 443 sont ouverts${NC}"
fi

# -----------------------------------------------------------------------------
# Création des répertoires nécessaires
# -----------------------------------------------------------------------------
echo ""
echo -e "${YELLOW}Création des répertoires pour SSL (pour plus tard)...${NC}"
mkdir -p certbot/conf certbot/www
echo -e "${GREEN}✓ Répertoires certbot créés${NC}"

# -----------------------------------------------------------------------------
# Résumé
# -----------------------------------------------------------------------------
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Configuration terminée !${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Prochaines étapes:"
echo "  1. Copiez le fichier .env.example vers .env"
echo "     cp .env.example .env"
echo ""
echo "  2. Éditez le fichier .env avec vos valeurs"
echo "     nano .env"
echo ""
echo "  3. Lancez le déploiement"
echo "     ./scripts/deploy.sh"
echo ""
