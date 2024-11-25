docker stop esps 
docker rm esps
docker run -d --name esps -v /data2/espsdata:/data2/espsdata -v /data2/minio_data:/data2/minio_data -p 5000:5000  -it esps
