import sys, os
import glob
import subprocess
import logging
from rembg import remove
from PIL import Image
import numpy as np
from pathlib import Path
import shutil


# 실행 경로 설정
exe_dir = Path(sys.executable).parent if getattr(sys, 'frozen', False) else Path(__file__).parent
meshroom_path = exe_dir / "Meshroom" / "meshroom_batch.exe"
graph_path = exe_dir / "Meshroom" / "custom_graph.mg"
glb_dir = exe_dir / "objects"

# === 로깅 설정: app.log는 상세하게, stdout은 간결하게 ===
root_logger = logging.getLogger()
root_logger.setLevel(logging.DEBUG) # 전체 로깅 레벨은 DEBUG로 유지

# 기존 핸들러 제거 (중복 방지 및 초기화)
for handler in root_logger.handlers[:]:
    root_logger.removeHandler(handler)

# 파일 핸들러 (app.log 파일에 상세 로그 기록)
file_handler = logging.FileHandler(exe_dir / "app.log")
file_handler.setLevel(logging.DEBUG) # 파일에는 모든 DEBUG 레벨 로그 기록
file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
root_logger.addHandler(file_handler)


# Meshroom을 위한 임시 디렉토리 환경 변수 설정
meshroom_env = os.environ.copy()
custom_meshroom_temp_path = exe_dir / "MeshroomTemp"
if not custom_meshroom_temp_path.exists():
    custom_meshroom_temp_path.mkdir(parents=True)
    logging.info(f"Meshroom 전용 임시 디렉토리 생성: {custom_meshroom_temp_path}")

meshroom_env["TEMP"] = str(custom_meshroom_temp_path)
meshroom_env["TMP"] = str(custom_meshroom_temp_path)
logging.info(f"Meshroom 프로세스에 TEMP/TMP 환경 변수 설정: {custom_meshroom_temp_path}")


# === 완전 검은 이미지 판별 함수 ===
def is_black_image(image_path: Path, threshold=5):
    try:
        img = Image.open(image_path).convert("L")
        arr = np.array(img)
        avg_brightness = arr.mean()
        return avg_brightness < threshold
    except Exception as e:
        logging.warning(f"이미지 판별 중 오류 발생: {image_path} - {e}")
        return False

# === 배경 제거 함수 ===
def remove_background(input_file: Path, output_file: Path, brightness_threshold=5):
    try:
        img = Image.open(input_file)
        out = remove(img)

        if out.mode == 'RGBA':
            out = out.convert('RGB')

        output_file_jpg = output_file.with_suffix('.jpg')
        out.save(output_file_jpg, format="JPEG", quality=100, subsampling=0, optimize=True)

        if is_black_image(output_file_jpg, threshold=brightness_threshold):
            output_file_jpg.unlink()
            logging.info(f"완전히 검은 이미지로 간주되어 삭제됨: {output_file_jpg}")

    except Exception as e:
        logging.warning(f"배경 제거 실패: {input_file} - {e}")


def convert_obj_to_glb(output_dir, oid):
    # exe_dir / "objects" 디렉토리가 없으면 생성
    if not glb_dir.exists():
        glb_dir.mkdir(parents=True)

    # convert_process.exe 호출, 이때 obj,mtl,png 파일이 있는 경로를 인자로 전달(output_dir)
    converter_exe = exe_dir / "convert_process.exe"
    if not converter_exe.exists():
        raise FileNotFoundError(f"변환기 exe를 찾을 수 없습니다: {converter_exe}")

    CREATE_NO_WINDOW = 0x08000000  # 윈도우용 콘솔창 숨김 플래그

    # convert_process.exe 실행 (/objects 경로 인자로 전달)
    result = subprocess.run(
        [str(converter_exe), str(output_dir), str(glb_dir), str(oid)],
        capture_output=True,
        text=True,
        creationflags=CREATE_NO_WINDOW,
    )
    logging.info(result.stdout)
    if result.returncode != 0:
        logging.error(f"변환 실패: {result.stderr}")
        return
    logging.info(f"변환 종료: {result.stdout}")



# === 전체 프로세스 처리 함수 ===
def process(input_dir: Path, oid):
    logging.info("프로세스 시작")
    logging.info(f"입력 디렉토리: {input_dir}")

    rembg_dir = input_dir / "rembg"
    output_dir = input_dir / "output"

    try:
        # 디렉토리 생성 (기존 폴더 제거 후 생성)
        if rembg_dir.exists():
            shutil.rmtree(rembg_dir)
        if output_dir.exists():
            shutil.rmtree(output_dir)
     

        rembg_dir.mkdir(parents=True, exist_ok=True)
        output_dir.mkdir(parents=True, exist_ok=True)


        image_files = list(input_dir.glob("*.jpg"))
        if not image_files:
            logging.warning("이미지 파일이 없습니다.")
            return

        for img_path in image_files:
            file_name = img_path.name
            out_path = rembg_dir / file_name

            logging.debug(f"배경 제거 시작: {img_path}")
            remove_background(img_path, out_path)
            logging.debug(f"배경 제거 완료: {img_path}")

        logging.info("모든 이미지 배경 제거 완료")

        valid_images = list(rembg_dir.glob("*.jpg"))
        if len(valid_images) < 10:
            logging.error("유효한 이미지 수가 너무 적습니다. 3D 재구성 불가능.")
            return

        # Meshroom 실행 명령어
        command = [
            str(meshroom_path.resolve()),
            "--input", str(rembg_dir.resolve()),
            "--output", str(output_dir.resolve()),
            "--pipeline", str(graph_path.resolve()) # 이 줄을 추가합니다.
        ]
        logging.info(f"Meshroom 실행 명령어: {' '.join(command)}")

        # subprocess.run을 사용하여 Meshroom 실행
        # shell=False (기본값) 유지, capture_output=True, text=True는 로그 캡처를 위해 유지
        meshroom_result = subprocess.run(
            command,
            check=False,
            capture_output=True,
            text=True,
            shell=False,
            creationflags = subprocess.SW_HIDE | subprocess.CREATE_NO_WINDOW, # 플래그 추가
            cwd=exe_dir,
            env=meshroom_env
        )

        logging.info(f"Meshroom 종료. 반환 코드: {meshroom_result.returncode}")
        if meshroom_result.stdout:
            for line in meshroom_result.stdout.splitlines():
                if line.strip():
                    logging.info(f"[Meshroom stdout] {line.strip()}")
        if meshroom_result.stderr:
            for line in meshroom_result.stderr.splitlines():
                if line.strip():
                    logging.error(f"[Meshroom stderr] {line.strip()}")

        if meshroom_result.returncode != 0:
            logging.error(f"Meshroom 실행 실패 (반환 코드: {meshroom_result.returncode}). 자세한 내용은 위의 Meshroom 출력을 확인하세요.")
        else:
            logging.info("3D 모델링 완료")
            logging.info("GLB 파일로 변환 시작")
            # GLB 변환 함수 호출
            logging.info(f"convert_obj_to_glb() 호출 - output_dir: {output_dir}, oid: {oid}")
            convert_obj_to_glb(output_dir, oid)


    except FileNotFoundError as e:
        logging.error(f"파일을 찾을 수 없습니다: {e}", exc_info=True)

    except FileExistsError as e:
        logging.error(f"파일이 이미 존재합니다: {e}")

    except subprocess.CalledProcessError as e:
        logging.error(f"Meshroom 실행 실패: {e}")

    except Exception as e:
        logging.exception("예외 발생")
        with open('error.log', 'a') as f:
            f.write(str(e) + '\n')


if __name__ == "__main__":
    if len(sys.argv) < 3:
        logging.error("입력 디렉토리 경로와 Unity 프로젝트 경로를 인자로 전달해야 합니다.")
        sys.exit(1)

    input_dir = Path(sys.argv[1])
    logging.info(f"사진 입력 디렉토리: {input_dir}")
    if not input_dir.exists():
        logging.error(f"입력 디렉토리가 존재하지 않습니다: {input_dir}")
        sys.exit(1)

    oid = sys.argv[2]
    logging.info(f"서버 디렉토리에 저장될 오브젝트 파일명: {oid}")
    if not oid:
        logging.error("오브젝트 파일명을 지정해야 합니다.")
        sys.exit(1)


    process(input_dir, oid)

