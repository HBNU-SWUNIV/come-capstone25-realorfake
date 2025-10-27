const obj2gltf = require('obj2gltf');
const fs = require('fs');
const path = require('path');

// exe 실행 경로에 따라 동적으로 log 파일 위치 설정
const exeDir = path.dirname(process.execPath); // 패키징된 exe의 위치
const logPath = path.join(exeDir, 'conv.log');

function log(message) {
  const timestamp = new Date().toISOString();
  const fullMessage = `${timestamp} - ${message}\n`;

  try {
    fs.appendFileSync(logPath, fullMessage, { encoding: 'utf8' });
  } catch (err) {
    console.error(`로그 기록 실패: ${err}`);
  }
}

async function main() {
    log(`process.argv.length: ${process.argv.length}`);

    const targetDirPath = process.argv[2];
    const glbDirPath = process.argv[3];
    const oid = process.argv[4];

    log(`프로그램 시작. 입력: ${targetDirPath}, 출력: ${glbDirPath}, oid: ${oid}`);

    if (!targetDirPath || !glbDirPath || !oid) {
        log('필수 인자가 부족합니다. 종료합니다.');
        process.exit(1);
    }

    if (!fs.existsSync(targetDirPath)) {
        log(`입력 경로가 존재하지 않습니다: ${targetDirPath}`);
        process.exit(1);
    }

    const files = fs.readdirSync(targetDirPath);
    const objFile = files.find(f => f.toLowerCase().endsWith('.obj'));

    if (!objFile) {
        log('obj 파일이 입력 디렉토리에 없습니다. 종료합니다.');
        process.exit(1);
    }

    const inputObjPath = path.join(targetDirPath, objFile);
    const outputGlbPath = path.join(glbDirPath, `${oid}.glb`);

    log(`변환 시작: ${inputObjPath} -> ${outputGlbPath}`);

    try {
        const glb = await obj2gltf(inputObjPath, {
            binary: true,
            embed: true
        });

        fs.writeFileSync(outputGlbPath, glb);
        log('변환 성공');
        process.exit(0);
    } catch (error) {
        log(`변환 실패: ${error.message || error}`);
        process.exit(1);
    }
}

main();
