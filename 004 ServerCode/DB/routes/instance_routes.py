from flask import Blueprint, send_file, request, jsonify, current_app, send_from_directory, abort
from models import Instance
from extensions import db
import os

instance_bp = Blueprint("instance", __name__, url_prefix="/instance")
UPLOAD_FILE_FOLDER = './uploads'
UPLOAD_IMG_FOLDER = './images'

# 오브젝트 정보 생성
@instance_bp.route("/create", methods=["POST"])
def create_instance():
    data = request.get_json()
    new_instance = Instance(
        oid=data["oid"],
        uid=data["uid"],
        # name=data["name"],
        bigClass=data["bigClass"],
        smallClass=data["smallClass"],
        abilityType=data["abilityType"],
        sellState=0,
        cost=-1,
        expireCount=data["expireCount"],
        stat=data["stat"],
        grade=data["grade"]
    )

    # 데이터베이스에 인스턴스 추가
    db.session.add(new_instance)
    db.session.commit()

    # 인스턴스의 oid 반환
    return jsonify({"message": "object saved"}), 201

# glb 파일 저장
@instance_bp.route('/upload/file', methods=['POST'])
def import_file():
    if 'file' not in request.files:
        return jsonify({"error": "No file part"}), 400

    file = request.files['file']
    if file.filename == '':
        return jsonify({"error": "No selected file"}), 400

    save_path = os.path.join(UPLOAD_FILE_FOLDER, file.filename)
    file.save(save_path)

    return jsonify({"message": "File uploaded", "filename": file.filename}), 200


# glb 파일 전송
# 요청을 보낸 대상에게 oid에 해당하는 glb파일 전송
# 파일 경로는 DB서버에서 DB/uploads/{oid}.glb
# glb 파일 전송
@instance_bp.route("/download/file/<int:oid>", methods=["GET"])
def export_file(oid):
    filename = f"{oid}.glb"
    file_path = os.path.join(UPLOAD_FILE_FOLDER, filename)

    if not os.path.exists(file_path):
        return abort(404, description=f"File {filename} not found.")

    return send_from_directory(directory=UPLOAD_FILE_FOLDER, path=filename, as_attachment=True, mimetype="model/gltf-binary")


# 생성된 물체 사진 이미지 저장
@instance_bp.route('/upload/img', methods=['POST'])
def import_img():
    if 'file' not in request.files:
        return jsonify({"error": "No file part"}), 400

    file = request.files['file']
    if file.filename == '':
        return jsonify({"error": "No selected Imgae"}), 400

    save_path = os.path.join(UPLOAD_IMG_FOLDER, file.filename)
    file.save(save_path)

    return jsonify({"message": "File uploaded", "filename": file.filename}), 200


# 생성된 물체 사진 이미지 전송
# 파일 경로는 DB서버에서 DB/imgs/{oid}.jpg
@instance_bp.route("/download/img/<int:oid>", methods=["GET"])
def export_img(oid):
    filename = f"{oid}.jpg"
    file_path = os.path.join(UPLOAD_IMG_FOLDER, filename)

    if not os.path.exists(file_path):
        return abort(404, description=f"Image {filename} not found.")

    return send_from_directory(directory=UPLOAD_IMG_FOLDER, path=filename, as_attachment=True, mimetype="image/jpeg")



# 오브젝트 정보 조회
@instance_bp.route('/<int:oid>', methods=['GET'])
def get_instance(oid):
    instance = Instance.get_by_id(oid)
    if not instance:
        return jsonify({'error': 'Not found'}), 404
    
    return jsonify({
        'oid': instance.oid,
        'uid': instance.uid,
        'bigClass': instance.bigClass,
        'smallClass': instance.smallClass,
        'abilityType': instance.abilityType,
        'sellState': instance.sellState,
        'cost': instance.cost,
        'expireCount': instance.expireCount,
        'stat': instance.stat,
        'grade': instance.grade
    })

# 오브젝트 정보 수정
@instance_bp.route('/<int:oid>', methods=['PUT'])
def update_instance(oid):
    instance = Instance.get_by_id(oid)
    if not instance:
        return jsonify({'error': 'Not found'}), 404
    
    data = request.get_json()
    print(data)
    
    # 수정할 필드만 업데이트
    if 'uid' in data and data['uid'] is not None:
        instance.uid = int(data['uid'])
    if 'name' in data and data['name'] is not None:
        instance.name = data['name']
    if 'bigClass' in data and data['bigClass'] is not None:
        instance.bigClass = data['bigClass']
    if 'smallClass' in data and data['smallClass'] is not None:
        instance.smallClass = data['smallClass']
    if 'abilityType' in data and data['abilityType'] is not None:
        instance.abilityType = data['abilityType']
    if 'sellState' in data and data['sellState'] is not None:
        instance.sellState = data['sellState']
    if 'cost' in data and data['cost'] is not None:
        instance.cost = int(data['cost'])
    if 'expireCount' in data and data['expireCount'] is not None:
        instance.expireCount = data['expireCount']
    if 'stat' in data and data['stat'] is not None:
        instance.stat = int(data['stat'])
    if 'grade' in data and data['grade'] is not None:
        instance.grade = data['grade']

    db.session.commit()

    return jsonify({'message': 'Instance updated successfully'})

# 오브젝트 정보 삭제
@instance_bp.route('/<int:oid>', methods=['DELETE'])
def delete_instance(oid):
    instance = Instance.get_by_id(oid)
    if not instance:
        return jsonify({'error': 'Not found'}), 404
    
    db.session.delete(instance)
    db.session.commit()

    return jsonify({'message': 'Instance deleted successfully'})




@instance_bp.route('/list/<int:uid>', methods=['GET'])
def get_user_instance_list(uid):
    items = Instance.query.filter_by(uid=uid).all()
    # 필요한 필드만 추출
    try:
        data = [item.oid for item in items]

        return jsonify({"success": True, "oid": data}), 200

    except Exception as e:
        db.session.rollback()
        return jsonify({"success": False, "error": str(e)}), 500

