#!/bin/sh
. ./buildSetting.txt
echo "//// $PROJECT_NAME BuildStart ////"
echo ""
mkdir -p ./$PROJECT_NAME/Build/
echo "//// Build Parameters ////"
echo ""
echo UnityPath : $1
echo WorkSpacePath : $2
echo TargetPlatform : $TARGET_PLATFORM
echo ApplicationName : $APPLICATION_NAME
echo BuildCommand : $BUILD_COMMAND
echo Run : $1 $BUILD_COMMAND
echo ""
echo "/////////////////////////"
$1 $BUILD_COMMAND