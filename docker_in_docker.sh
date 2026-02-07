#!/bin/bash
set -e

echo "Installing dependencies..."
apt-get update
apt-get install -y ca-certificates curl gnupg lsb-release

# 2. Add Docker's official GPG key
echo "Adding Docker GPG key..."
mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor --yes -o /etc/apt/keyrings/docker.gpg

# 3. Set up the repository
# Ubuntu 24.04 is 'noble'
DISTRO=$(lsb_release -cs)
if [ -z "$DISTRO" ]; then
    DISTRO="noble"
fi

echo "Setting up Docker repository for $DISTRO..."
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $DISTRO stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null

# 4. Install Docker Engine
echo "Installing Docker Engine..."
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# 5. Function to start docker daemon
start_docker() {
    echo "Starting Docker daemon..."
    rm -f /var/run/docker.pid
    
    # Start dockerd in background
    # --iptables=false and --bridge=none are common workarounds if the container lacks network permissions
    dockerd --iptables=false > /tmp/dockerd.log 2>&1 &

    # Wait for docker to start
    MAX_RETRIES=15
    RETRIES=0
    until docker info >/dev/null 2>&1 || [ $RETRIES -eq $MAX_RETRIES ]; do
      echo "Waiting for Docker daemon to start... ($RETRIES/$MAX_RETRIES)"
      sleep 3
      ((RETRIES++))
    done

    if docker info >/dev/null 2>&1; then
      echo "---------------------------------------------------------"
      echo "SUCCESS: Docker daemon is ready."
      echo "---------------------------------------------------------"
    else
      echo "---------------------------------------------------------"
      echo "ERROR: Failed to start Docker daemon."
      echo "This is likely because the container is NOT running with --privileged."
      echo "Check /tmp/dockerd.log for details."
      echo "---------------------------------------------------------"
      exit 1
    fi
}

start_docker