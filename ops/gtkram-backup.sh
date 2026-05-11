#!/bin/sh

DAY="$(date +'%d')"
BACKUPFILE="gtkram-app-$DAY.7z"
BACKUPDIR="/opt/backup"

echo "remove old file"
rm -f "$BACKUPDIR/$BACKUPFILE" || true

echo "create backup"
7z a -mhe=on "$BACKUPDIR/$BACKUPFILE" /opt/gtkram/ -p"***" > /dev/null 2>&1

echo "sync backup"
rclone sync --exclude gtkram-backup.sh $BACKUPDIR remote:gtkram-app
