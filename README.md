# Mono static linking example


Linux:
* Install mono
* Install zlib1g-dev

OSx: 
* Install mono
* Set environment:

```
export PATH=/Library/Frameworks/Mono.framework/Commands:$PATH
export AS="as -arch i386"
export CC="cc -arch i386 -framework CoreFoundation -lobjc -liconv"
```

Building

```
./build.sh
```

Running

```
./build/myappd
```
