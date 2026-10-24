FROM lscr.io/linuxserver/webtop:ubuntu-xfce

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        foot \
        dnsutils \
        git \
        htop \
        iproute2 \
        iputils-ping \
        less \
        man-db \
        manpages \
        nano \
        netcat-openbsd \
        procps \
        tree \
        unzip \
        vim-tiny \
        wget \
        zip && \
    rm -rf /var/lib/apt/lists/*

