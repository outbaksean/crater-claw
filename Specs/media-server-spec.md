# Spec: media-server

## Goal

Set up Jellyfin as the household media server, running in a Proxmox LXC on the minipc. The external hard drive attached to the minipc holds the media library. An SMB share on the LXC gives CraterClaw direct file access for the media library plugin. DLNA replaces the existing DLNA setup. The Jellyfin REST API is available for a future CraterClaw plugin.

This is an infrastructure checkpoint — no code is written. The output is a running server and a set of configuration values needed for downstream coding checkpoints.

---

## Phase 1: Proxmox LXC and storage

**Status:** Pending

### Steps

1. **Format and label the external drive** (if not already done)
    - Attach the drive to the minipc
    - Partition and format as ext4 (or reuse existing filesystem)
    - Note the UUID: `blkid /dev/sdX1`

2. **Mount the drive on the Proxmox host**
    - Create mount point: `mkdir -p /mnt/media`
    - Add fstab entry for persistence:
        ```
        UUID=<uuid> /mnt/media ext4 defaults,nofail 0 2
        ```
    - Mount: `mount -a` and verify with `df -h`

3. **Create the LXC**
    - In the Proxmox web UI or via `pct`: create a Debian or Ubuntu LXC
    - Recommended: unprivileged container
    - Assign a static IP on the LAN (or reserve one via DHCP by MAC)
    - Give it a memorable hostname (e.g. `mediaserver`)
    - Note the container ID (e.g. `100`)

4. **Bind-mount the drive into the LXC**
    - Add to `/etc/pve/lxc/<id>.conf`:
        ```
        mp0: /mnt/media,mp=/media
        ```
    - For an unprivileged container, uid/gid mapping may be needed. If Jellyfin or Samba cannot write to `/media`, add to the LXC conf:
        ```
        lxc.idmap: u 0 100000 65536
        lxc.idmap: g 0 100000 65536
        ```
        And ensure the host mount directory is owned by uid 100000:
        ```
        chown -R 100000:100000 /mnt/media
        ```
    - Restart the LXC after editing the conf file

5. **Create the directory structure inside the LXC**
    ```
    mkdir -p /media/movies
    ```
    Movies are stored flat inside `/media/movies/` — no subdirectories.

### Verification

- LXC starts cleanly
- `/media/movies/` is writable from inside the LXC
- A test file written inside the LXC appears on the Proxmox host at `/mnt/media/movies/`

---

## Phase 2: Jellyfin

**Status:** Pending

### Steps

1. **Install Jellyfin** inside the LXC
    - Follow the official Jellyfin install guide for Debian/Ubuntu:
        ```
        curl https://repo.jellyfin.org/install-jellyfin.py | python3
        ```
    - Enable and start the service:
        ```
        systemctl enable --now jellyfin
        ```

2. **Initial Jellyfin setup** via the web UI at `http://<lxc-ip>:8096`
    - Create an admin account — note the username and password
    - Add a Movie library pointing at `/media/movies`
    - Set the preferred metadata language and region

3. **Generate an API key**
    - Dashboard > API Keys > Add
    - Note the key — needed for `jellyfin-api-plugin`

4. **Configure DLNA** (replaces existing DLNA server)
    - Dashboard > DLNA > Enable DLNA server
    - Verify existing DLNA clients discover the new server
    - Decommission the old DLNA server once verified

### Details to record

| Detail            | Value                  |
| ----------------- | ---------------------- |
| Jellyfin base URL | `http://<lxc-ip>:8096` |
| Jellyfin API key  | (from step 3)          |
| Admin username    |                        |

### Verification

- Jellyfin web UI loads at `http://<lxc-ip>:8096`
- Movies library shows (empty is fine)
- A test `.mkv` copied into `/media/movies/` appears in the library after a scan
- DLNA clients on the LAN discover Jellyfin

---

## Phase 3: SMB share

**Status:** Pending

### Steps

1. **Install Samba** inside the LXC

    ```
    apt install samba -y
    ```

2. **Configure the share** in `/etc/samba/smb.conf`

    ```ini
    [media]
        path = /media
        browseable = yes
        read only = no
        guest ok = yes
    ```

    Adjust `guest ok` and add user authentication if the LAN is not trusted.

3. **Restart Samba**

    ```
    systemctl restart smbd nmbd
    ```

4. **Verify access from Windows**
    - Open File Explorer, navigate to `\\<lxc-ip>\media` or `\\mediaserver\media`
    - Confirm `movies\` directory is visible and writable

### Details to record — needed for `media-library-config`

| Detail                   | Value                          |
| ------------------------ | ------------------------------ |
| UNC path to library root | `\\<lxc-ip-or-hostname>\media` |
| Category: movies         | `movies`                       |

### Verification

- `\\<lxc-hostname>\media\movies\` is accessible from the Windows dev machine
- A file copied via SMB appears in Jellyfin after a library scan
- `CRATERCLAW_CONFIG` or `craterclaw.json` can be updated with the UNC path and the `media-library-config` coding checkpoint can begin
