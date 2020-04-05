# 程式說明

* SampleWeb為要監控的網站
* WebMonitor為監控的站台，get /metrics會取得Prometheus格式的效能指標資料
* dotnetdiag中dockerfile可以建立一個含有dotnet-counters工具的image，可以使用以下命令進行monitor操作
```
docker run -it --rm -v <volumeName>:/tmp docker.pkg.github.com/phoenix-chen-2016/netcoremonitorexample/dotnetdiag:3.1
```
* dotnetdiag中docker-compose.yml則是整個sample的配置，執行起來後可以在 http://localhost:8088/metrics 中看到結果。