FROM lscr.io/linuxserver/webtop:ubuntu-xfce

RUN apt-get update && \
    apt-get install -y iputils-ping iproute2 netcat-openbsd && \
    rm -rf /var/lib/apt/lists/*

RUN mkdir -p /custom-cont-init.d && \
    echo '#!/bin/bash' > /custom-cont-init.d/99-setup-desktop.sh && \
    echo 'mkdir -p /config/Desktop' >> /custom-cont-init.d/99-setup-desktop.sh && \
    echo 'cp /usr/share/applications/xfce4-terminal.desktop /config/Desktop/' >> /custom-cont-init.d/99-setup-desktop.sh && \
    echo 'cp /usr/share/applications/thunar.desktop /config/Desktop/' >> /custom-cont-init.d/99-setup-desktop.sh && \
    echo 'chmod +x /config/Desktop/*.desktop' >> /custom-cont-init.d/99-setup-desktop.sh && \
    echo 'chown -R abc:abc /config/Desktop' >> /custom-cont-init.d/99-setup-desktop.sh && \
    chmod +x /custom-cont-init.d/99-setup-desktop.sh