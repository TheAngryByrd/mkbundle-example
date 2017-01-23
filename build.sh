#!/bin/bash
if test "$OS" = "Windows_NT" 
then
  # use .Net

  .paket/paket.bootstrapper.exe
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
  	exit $exit_code
  fi

  .paket/paket.exe restore
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
  	exit $exit_code
  fi

  packages/FAKE/tools/FAKE.exe $@ --fsiargs build.fsx
else
  if [[ "$OSTYPE" == "darwin"* ]];
  then
    if [[ $PATH == /Library/Frameworks/Mono.framework/Commands* ]] 
    then
      echo "all good"
    else
       export PATH=/Library/Frameworks/Mono.framework/Commands:$PATH 
    fi
    
    export AS="as -arch i386"
    export CC="cc -arch i386 -framework CoreFoundation -lobjc -liconv"
  fi
  # use mono
  mono .paket/paket.bootstrapper.exe
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
  	exit $exit_code
  fi

  mono .paket/paket.exe restore
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
  	exit $exit_code
  fi
  mono packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO build.fsx
fi
