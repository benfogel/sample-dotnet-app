#!/bin/bash

# Start the dotnet application in the background
dotnet app.dll &

# Start FIO to generate load and log the output
fio --name=standard-test --filename=/app/logs/testfile.dat --ioengine=libaio --size=G --verify=0 --randrepeat=0 --bs=4k --direct=1 --rw=randrw --rate=51200k,51200k --time_based --runtime=7d --numjobs=1 --iodepth=32 --output=/app/logs/fio-results.log

# Wait for any process to exit
wait -n

# Exit with status of process that exited first
exit $?
