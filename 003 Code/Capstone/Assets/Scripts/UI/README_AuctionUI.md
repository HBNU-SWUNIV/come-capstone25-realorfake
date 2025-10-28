# AuctionUI 사용법

## 개요
AuctionUI는 경매 시스템의 메인 UI를 관리하는 스크립트입니다. JSON 데이터를 기반으로 Card(UI) 프리팹을 생성하고, 페이지네이션과 DetailPage 전환 기능을 제공합니다.

## 주요 기능

### 1. JSON 데이터 로드
- `Resources/JSON/TestJSON/test.json` 파일에서 카드 데이터를 자동으로 로드
- 36개의 카드 아이템 정보를 포함 (oid 1~36)

### 2. 카드 배치 시스템
- **LeftView**: oid 1~12, 25~36, 49~60... (12개씩)
- **RightView**: oid 13~24, 37~48, 61~72... (12개씩)
- 각 뷰는 스크롤 가능한 ScrollRect로 구성

### 3. 페이지네이션
- **NextButton**: 다음 페이지로 이동 (LeftView: 25~36, RightView: 37~48)
- **PrevButton**: 이전 페이지로 이동
- 버튼 상태는 자동으로 업데이트 (첫/마지막 페이지에서 비활성화)

### 4. DetailPage 전환
- **Registation 버튼**: Main 패널을 비활성화하고 DetailPage 활성화
- DetailPage에서 Back 버튼으로 Main 패널로 복귀

## 설정 방법

### 1. AuctionUI 컴포넌트 설정
```
UI References:
- mainPanel: Main 패널 GameObject
- detailPage: DetailPage GameObject  
- leftView: LeftView ScrollRect
- rightView: RightView ScrollRect
- nextButton: NextButton Button
- prevButton: PrevButton Button
- registationButton: Registation Button

Card Settings:
- cardPrefab: Card(UI) 프리팹
- leftViewContent: LeftView의 Content Transform
- rightViewContent: RightView의 Content Transform

Pagination:
- itemsPerPage: 페이지당 아이템 수 (기본값: 12)
- currentPage: 현재 페이지 (기본값: 0)
```

### 2. DetailPageUI 컴포넌트 설정
```
UI References:
- backButton: DetailPage의 뒤로가기 버튼
- mainPanel: Main 패널 GameObject (AuctionUI와 동일한 객체)
```

### 3. Card(UI) 프리팹 요구사항
- CardDisplay 컴포넌트가 포함되어 있어야 함
- Button 컴포넌트가 포함되어 있어야 함 (클릭 이벤트용)

## JSON 데이터 구조
```json
{
    "oid": "1",
    "uid": "0", 
    "bigClass": "Kitchen",
    "smallClass": "cup",
    "abilityType": "A",
    "sellState": "N",
    "cost": "0",
    "expireCount": "-1",
    "stat": "10",
    "grade": "Normal"
}
```

## 동작 흐름

1. **시작**: JSON 데이터 로드 → 버튼 설정 → 첫 페이지 표시
2. **페이지네이션**: Next/Prev 버튼 클릭 → 카드 재배치 → 버튼 상태 업데이트
3. **DetailPage 전환**: Registation 버튼 클릭 → Main 패널 비활성화 → DetailPage 활성화
4. **복귀**: DetailPage의 Back 버튼 클릭 → DetailPage 비활성화 → Main 패널 활성화

## 디버그 기능
- 화면 좌상단에 현재 페이지 번호 표시
- 콘솔에 로드된 아이템 수와 페이지 정보 출력
- 카드 클릭 시 콘솔에 클릭된 카드 정보 출력

## 주의사항
- Resources 폴더에 JSON 파일이 올바르게 위치해야 함
- Card(UI) 프리팹에 필요한 컴포넌트들이 포함되어 있어야 함
- ScrollRect의 Content Transform을 올바르게 설정해야 함
