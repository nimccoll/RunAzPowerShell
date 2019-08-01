FROM debian:jessie

# Install system components
RUN apt-get update -y
RUN apt-get install -y curl apt-transport-https

# Import the public repository GPG keys
RUN curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -

# Register the Microsoft Product feed
RUN sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-jessie-prod jessie main" > /etc/apt/sources.list.d/microsoft.list'

# Update the list of products
RUN apt-get update -y

# Install PowerShell
RUN apt-get install -y powershell

# Install Azure CLI
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash

# Copy application files
ADD app /

# Gran execute permission to the application
RUN chmod +x RunAzPowerShell

# Execute application
CMD RunAzPowerShell