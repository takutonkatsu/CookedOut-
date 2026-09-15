import fs from 'node:fs/promises';
import path from 'node:path';
import { FileBlob, SpreadsheetFile, Workbook } from '@oai/artifact-tool';

const root = '/Users/takuto/development/CookedOut';
const dataPath = path.join(root, 'docs/research/data/overcooked2_base_reference.json');
const reportPath = path.join(root, 'docs/research/17_overcooked2_base_game_complete_reference.md');
const outputDir = path.join(root, 'outputs/01a08953-a33c-7433-8d93-0ea64e0c4372');
const workbookPath = path.join(outputDir, 'overcooked2_base_game_reference.xlsx');
const previewDir = '/tmp/cookedout-overcooked2-reference-previews';

const layoutJa = {
  'Tutorial': '上下の角にまな板2台。下辺に食材箱3種、皿、返却口を横一列に置いた固定・対称寄りの基礎盤面。',
  '1-1': '魚箱を左、エビ箱を右、皿4枚を中央島、まな板4台を下辺左右に分けた、短い往復だけの固定盤面。',
  '1-2': '食材箱3種を中央、まな板を左上、提供・返却を右上、鍋を右下へ置く。中央通路を歩行者が横断する。',
  '1-3': '左側にエビ・海苔・米、右側にキュウリ。左上にまな板、右上に鍋、下辺の両端に提供と洗い場を分離。',
  '1-4': '上辺と下辺を横断するコンベアが中央の狭路を囲む。食材は左上、まな板は右上、鍋は右、洗い場は左。',
  '1-5': '大小2基の気球厨房を中央通路で接続。左の大型気球に切る・煮る・炒める、右の小型気球に洗う・提供を集約。まな板列が上下移動。',
  '1-6': '正方形の気球でサラダを作り、経過2分20秒で寿司店へ墜落。前半は中央火炎、落下中は設備が滑り、後半は外周コンベアと鍋が追加。',
  '2-1': '上下2隻のいかだ。上はいかだ全幅のまな板、下はいかだにフライヤー・食材箱・提供口を並べ、接続距離が変わる。',
  '2-2': '左の提供気球、上下移動する中央の材料・まな板気球、右の鍋・フライパン気球の3分割。中央が唯一の受け渡し橋になる。',
  '2-3': '左右の大型気球を、4マスの小型移動足場が往復接続。左に材料・洗い・提供、右に鍋・フライパン・皿を集中。',
  '2-4': '橋1本で結んだ左右2区画。左に鍋・トルティーヤ・キノコ・提供、右にまな板・フライパン・米・肉・洗い場。',
  '2-5': '左右2区画を中央レールで接続し、食材箱列が左右へ移動。左は鍋・まな板・洗い、右はフライパン・提供。',
  '2-6': '中央の縦長カウンターが30秒ごとに90度回転し、フライパン3台と皿4枚のアクセス先を変える。周囲に材料・まな板・洗い・提供を分散。',
  '3-1': '下段左右の間をまな板3台付きカウンターが約30秒ごとに移動。上段の提供へは階段で上がり、左に材料、右にオーブンと洗い場。',
  '3-2': '左・中央・右の3区画。中央階段が35秒ごとに接続先を左右交替し、左右はポータルでも連絡。材料・加熱・提供を別区画へ分ける。',
  '3-3': '4象限の厨房を、食材箱付き可動カウンターが縦分断と横分断へ約30〜40秒周期で切替。各象限に主要設備を1種類ずつ配置。',
  '3-4': '中央高台の提供区画へ左右からポータル接続。左にまな板とフライパン、右に食材と洗い場。左右間の裂け目は投擲のみ通過可能。',
  '3-5': '左右2隻のいかだが離合集散。左に食材・フライヤー・提供、右にまな板・洗い場。皿は2枚だけで、皿物流が主な制約。',
  '3-6': '上・下の固定区画を、全食材箱を載せた中央足場が振り子状に接続。経過2分20秒で3区画がいかだ化し、川上を移動する。',
  '4-1': '車道で左右分断。左は米・キュウリ・鍋・まな板・洗い、右は魚介供給と提供。信号に合わせて車が通り、横断に危険がある。',
  '4-2': '2隻のいかだが縦接続と横接続を交互に切替。L字コンベアだけが皿を相手側へ渡せ、揚げ・サラダ・洗い・提供を両側へ分散。',
  '4-3': '左右2区画をS字コンベアで接続し、ベルト方向が30秒ごとに反転。左に材料・洗い・提供、右にまな板・フライパン。',
  '4-4': '左右区画を上下2本の橋で接続。上下で逆向きの強風が約13秒ごとに反転し、外側の短いカウンターも同周期で上下する。',
  '4-5': '上下2区画を外周ベルトと中央の重なったL字ベルトで接続。各側のボタンで搬送先を切替。鍋・提供と、切る・洗うを分離し皿は2枚。',
  '4-6': '左右2区画を上下の歩行可能コンベアで接続。左右壁の設備・材料カウンターが30秒ごとに入替わり、通路上には火災も発生。',
  '5-1': '左右岸を、皿3枚と操作レバーを載せた中央可動橋で接続。左に鍋・フライパン・材料、右にまな板・洗い・提供。',
  '5-2': '2つの作業島を外周回廊で囲み、提供口を最左端に配置。中央の炉から発生する火が回廊と作業口を断続的に塞ぐ。',
  '5-3': '穴の中央に操作可能な小足場を置き、円周上の鍋・フライパン・材料・まな板・洗い・提供へ順番に寄せて作業する。',
  '5-4': '左上・右上・下の3区画。約35秒ごとに階段が消えてポータルへ切替。下から出る移動ポータルは穴上にも動き、落下リスクがある。',
  '5-5': '四隅を中央壁で左右分断。左右それぞれの小足場を反対側のレバーで動かすため、相互操作なしでは材料と設備を結べない。',
  '5-6': '3段階変形。開始時は3島を小足場が接続、残り3:48で床が連結しカウンター移動、残り1:58で左右分断とポータル構成へ再編。',
  '6-1': '上・中央・下の3作業帯を左のコンベアと右の回廊で接続。高い壁で中央への投擲を封じ、混合・焼成・洗い・提供を別帯に置く。',
  '6-2': '左・右・上の3島。接続床が約13秒浮上、約5秒沈下し、形状も順番に変化。混合・焼成・材料・提供を島ごとに分散。',
  '6-3': '食材ベルトは通常そのままゴミ箱へ流れる。左右のボタンで必要食材だけを受取側へ分岐し、四隅に混合・焼成・加熱・提供を配置。',
  '6-4': '気球上のほぼ対称厨房。2本のレバーで、それぞれ4食材箱を載せた2×2足場を外周へ動かし、中央設備への供給位置を変える。',
  '6-5': '上下チームを固定分断し、中央の環状コンベアで材料と皿を授受。各半分の左右区画が45秒ごとに交互沈下する。',
  '6-6': '注文達成で厨房そのものが段階変形。前半は切る・鍋・フライパン・揚げ物、後半はポータルとコンベアを使う混合・焼成へ移行。提供口はゲート制御。',
  'Kevin 1': '左右完全分断。左にミキサー・洗い場・牛肉、右に蒸し器・提供・小麦粉・魚。中央カウンター上の皿で中間物を受渡す。',
  'Kevin 2': '中央カウンターの周囲に4つの3×3島。残り3:00から約23秒ごとに全島が時計回り／反時計回りへ交互移動し、設備の隣接関係が変わる。',
  'Kevin 3': '下段と2つの高台を昇降機1基で接続。下段に混合・材料・提供、高台にまな板・蒸し器・洗い場を分散。',
  'Kevin 4': '中央の大型暖炉を囲む長方形回廊。暖炉が全方向へ火を射出。左右にミキサーと蒸し器、上に切る・提供、下に全材料を配置。',
  'Kevin 5': '上下2本の2×5作業台の間を車が横断。風が作業台を左右交換し、同時にプレイヤーも押す。混合と蒸しを別作業台へ分離。',
  'Kevin 6': '4象限を時計回りコンベアで連結。蒸す・切る・材料・混合を象限ごとに分離し、中央または外周向きの入口と少数皿で物流を制約。',
  'Kevin 7': '上下2区画を中央1マスだけで接続。左右の可動足場にミキサー／蒸し器を載せ、材料と皿も上下に分散した強いボトルネック構造。',
  'Kevin 8': '左の蒸し区画と右の混合区画を高速ベルトで完全分断。5本の食材ベルトが1本へ合流後3本へ分岐し、回転分岐の先はゴミ箱。',
};

const familyJa = {
  'Salad': 'サラダ', 'Sashimi': '刺身', 'Sushi': '寿司', 'Pasta': 'パスタ',
  'Fast Food': '揚げ物', 'Burrito': 'ブリトー', 'Burritos': 'ブリトー',
  'Burger': 'バーガー', 'Pizza': 'ピザ', 'Pancake': 'パンケーキ',
  'Cake': 'ケーキ', 'Steamed Food': '蒸し料理'
};

const recipes = [
  ['サラダ', 'レタスサラダ', 'レタス', 1, 0],
  ['サラダ', 'トマトサラダ', 'レタス＋トマト', 2, 0],
  ['サラダ', 'キュウリ入りサラダ', 'レタス＋トマト＋キュウリ', 3, 0],
  ['刺身', '魚の刺身', '魚', 1, 0],
  ['刺身', 'エビの刺身', 'エビ', 1, 0],
  ['寿司', '魚寿司', '海苔＋米＋魚', 3, 0],
  ['寿司', 'キュウリ寿司', '海苔＋米＋キュウリ', 3, 0],
  ['寿司', '魚キュウリ寿司', '海苔＋米＋魚＋キュウリ', 4, 0],
  ['パスタ', 'トマトパスタ', 'パスタ＋トマト', 2, 20],
  ['パスタ', '牛肉パスタ', 'パスタ＋牛肉', 2, 20],
  ['パスタ', 'キノコパスタ', 'パスタ＋キノコ', 2, 20],
  ['パスタ', '魚介パスタ', 'パスタ＋魚＋エビ', 3, 20],
  ['揚げ物', 'チキンナゲット', '鶏肉', 1, 20],
  ['揚げ物', 'フライドポテト', 'ジャガイモ', 1, 20],
  ['揚げ物', 'ナゲット＆ポテト', '鶏肉＋ジャガイモ', 2, 20],
  ['ブリトー', '牛肉ブリトー', 'トルティーヤ＋米＋牛肉', 3, 20],
  ['ブリトー', '鶏肉ブリトー', 'トルティーヤ＋米＋鶏肉', 3, 20],
  ['ブリトー', 'キノコブリトー', 'トルティーヤ＋米＋キノコ', 3, 20],
  ['バーガー', 'ミートバーガー', 'バンズ＋牛肉', 2, 0],
  ['バーガー', 'チーズバーガー', 'バンズ＋牛肉＋チーズ', 3, 0],
  ['バーガー', 'レタスチーズバーガー', 'バンズ＋牛肉＋レタス＋チーズ', 4, 0],
  ['バーガー', 'トマトレタスバーガー', 'バンズ＋牛肉＋トマト＋レタス', 4, 0],
  ['ピザ', 'チーズピザ', '生地＋トマト＋チーズ', 3, 20],
  ['ピザ', 'ペパロニピザ', '生地＋トマト＋チーズ＋ペパロニ', 4, 20],
  ['ピザ', 'チキンピザ', '生地＋トマト＋チーズ＋鶏肉', 4, 20],
  ['パンケーキ', 'プレーンパンケーキ', '小麦粉＋卵', 2, 20],
  ['パンケーキ', 'チョコパンケーキ', '小麦粉＋卵＋チョコ', 3, 20],
  ['ケーキ', 'ハニーケーキ', '小麦粉＋卵＋ハチミツ', 3, 40],
  ['ケーキ', 'キャロットケーキ', '小麦粉＋卵＋ハチミツ＋ニンジン', 4, 40],
  ['ケーキ', 'チョコケーキ', '小麦粉＋卵＋ハチミツ＋チョコ', 4, 40],
  ['蒸し料理', '蒸し魚', '魚', 1, 20],
  ['蒸し料理', '牛肉団子', '小麦粉＋牛肉', 2, 40],
  ['蒸し料理', 'エビ団子', '小麦粉＋エビ', 2, 40],
  ['蒸し料理', 'ニンジン団子', '小麦粉＋ニンジン', 2, 40],
];

const timings = [
  ['切る', '切断対象すべて', 'レタス、トマト、キュウリ、魚、エビ、肉、鶏、ジャガイモ、キノコ、チーズ、生地、ペパロニ、チョコ、ニンジン、ハチミツ', '約7秒（ソロ）', '協力は6クリック／別資料では約5秒', '中', 'OC2/AYCE Tech v2.2、Steam中国語ガイド'],
  ['煮る', '鍋・直火', '米、パスタ', '12秒', '完成後から焦げまで同じ12秒', '中〜高', 'OC2/AYCE Tech v2.2'],
  ['焼く／炒める', 'フライパン・直火', '牛肉、鶏肉、キノコ、パスタ具、パンケーキ生地', '12秒', '完成後から焦げまで同じ12秒', '中〜高', 'OC2/AYCE Tech v2.2'],
  ['蒸す', '蒸し器・直火', '魚、各種団子', '12秒', '直火器具の共通値として整理', '中', 'OC2/AYCE Tech v2.2'],
  ['混ぜる', 'ミキサー', '小麦粉＋卵、団子生地、ケーキ生地', '12秒（厳密には約12〜12.5秒）', '全材料を最初に入れれば材料数に依存しない', '中〜高', 'Cooking / Mixing Times Analysis、OC2/AYCE Tech v2.2'],
  ['揚げる', 'フライヤー', '鶏肉、ジャガイモ', '10秒', '完成後から焦げまで同じ10秒', '中〜高', 'OC2/AYCE Tech v2.2'],
  ['焼成', 'オーブン', 'ピザ、ケーキ', '10秒', '完成後から焦げまで同じ10秒', '中〜高', 'OC2/AYCE Tech v2.2'],
  ['皿洗い', 'シンク', '汚れ皿', '約5秒', '複数人で同時洗浄可能', '中', 'Steamコミュニティ実測ガイド'],
];

const sources = [
  ['公式', 'Team17: Overcooked! 2 FAQ', 'https://www.team17.com/news/overcooked-2-faq', '45厨房、Dynamic Kitchen、New Game+の定義', '高'],
  ['公式', 'Team17: The A-Z of Overcooked! 2', 'https://www.team17.com/news/the-a-z-of-overcooked-2', '45厨房、4つ星モードの解放説明', '高'],
  ['コミュニティ集計', 'Score Requirements for Every Level', 'https://steamcommunity.com/sharedfiles/filedetails/?id=1832823209', 'Steam版1〜4人・星1〜4条件。ブックの採用値', '中〜高'],
  ['コミュニティWiki', 'Overcooked Wiki individual level pages', 'https://overcooked.fandom.com/wiki/Overcooked!_2', '制限時間、料理、設備配置、周期変化', '中〜高'],
  ['コミュニティWiki', 'overcooked2 @ ウィキ：スコア', 'https://w.atwiki.jp/overcooked2/pages/18.html', 'チップ3〜8、倍率、失注ペナルティ', '中'],
  ['コミュニティWiki', 'overcooked2 @ ウィキ：料理', 'https://w.atwiki.jp/overcooked2/pages/16.html', '料理系統の基本点レンジ', '中'],
  ['上級者実測', 'OC2/AYCE Tech v2.2', 'https://steamcommunity.com/sharedfiles/filedetails/?id=2456165919', '切る・混ぜる・加熱・焦げ猶予・注文生成周期', '中〜高'],
  ['プレイヤー実測', 'Cooking / Mixing Times Analysis', 'https://www.reddit.com/r/OvercookedGame/comments/c66t2v/', '混合12〜12.5秒と途中投入時の進捗挙動', '中'],
  ['プレイヤー解析', 'OC2 Scoring Calculations', 'https://www.reddit.com/r/OvercookedGame/comments/lt1o1g/', '料理点の推定式と工程複雑度加点', '中'],
];

const colors = {
  navy: '#24344D', blue: '#496A8B', pale: '#EAF0F6', gold: '#E8B04E',
  cream: '#FFF8E8', ink: '#24313E', muted: '#66717D', white: '#FFFFFF',
  grid: '#D8DEE6', red: '#A94442', green: '#3B7A57'
};

function jpRecipes(value) {
  return String(value || '').split(',').map((x) => familyJa[x.trim()] || x.trim()).join('・');
}

function stageKind(stage) {
  if (stage === 'Tutorial') return '導入';
  if (stage.startsWith('Kevin')) return 'Kevin';
  return '本編';
}

function parseTimes(record) {
  if (record.stage === '6-6') return { solo: 1500, coop: 900, display: 'ソロ25:00／協力15:00' };
  const match = String(record.time).match(/(\d+):(\d+)/);
  const seconds = match ? Number(match[1]) * 60 + Number(match[2]) : null;
  return { solo: seconds, coop: seconds, display: record.time };
}

function median(numbers) {
  const values = [...numbers].sort((a, b) => a - b);
  const middle = Math.floor(values.length / 2);
  return values.length % 2 ? values[middle] : (values[middle - 1] + values[middle]) / 2;
}

function styleTitle(sheet, title, subtitle, endColumn) {
  sheet.showGridLines = false;
  sheet.getRange('A2').values = [[title]];
  sheet.getRange('A2').format.font = { name: 'Aptos', size: 16, bold: true, color: colors.ink };
  sheet.getRange('A3').values = [[subtitle]];
  sheet.getRange('A3').format.font = { name: 'Aptos', size: 10, italic: true, color: colors.muted };
  sheet.getRange('A4:' + endColumn + '4').format.borders = {
    bottom: { style: 'thin', color: colors.blue }
  };
}

function styleTable(sheet, rangeAddress, headerAddress) {
  const body = sheet.getRange(rangeAddress);
  body.format.font = { name: 'Aptos', size: 10, color: colors.ink };
  body.format.verticalAlignment = 'center';
  const header = sheet.getRange(headerAddress);
  header.format.fill = colors.navy;
  header.format.font = { name: 'Aptos', size: 10, bold: true, color: colors.white };
  header.format.horizontalAlignment = 'center';
  header.format.verticalAlignment = 'center';
  header.format.borders = {
    insideVertical: { style: 'thin', color: colors.white },
    bottom: { style: 'medium', color: colors.navy }
  };
}

function makeWorkbook(data) {
  const wb = Workbook.create();
  const summary = wb.worksheets.add('概要');
  const stages = wb.worksheets.add('ステージ一覧');
  const scores = wb.worksheets.add('星スコア');
  const recipeSheet = wb.worksheets.add('料理・基本点');
  const timingSheet = wb.worksheets.add('工程時間');
  const raw = wb.worksheets.add('原文盤面記述');
  const sourceSheet = wb.worksheets.add('出典');

  styleTitle(summary, 'Overcooked! 2 本編 完全参照表', '対象: Tutorial + 本編36面 + Kevin 8面。星条件はPC/Steam版コミュニティ集計を採用。', 'H');
  summary.getRange('A6:B13').values = [
    ['指標', '値'],
    ['厨房数', null],
    ['本編面数', null],
    ['Kevin面数', null],
    ['料理系統数', 11],
    ['完成料理バリエーション', recipes.length],
    ['制限時間中央値（秒）', null],
    ['4★/3★倍率中央値（全人数・全厨房）', null],
  ];
  summary.getRange('B7').formulas = [[`=COUNTA('ステージ一覧'!B7:B51)`]];
  summary.getRange('B8').formulas = [[`=COUNTIF('ステージ一覧'!A7:A51,"本編")`]];
  summary.getRange('B9').formulas = [[`=COUNTIF('ステージ一覧'!A7:A51,"Kevin")`]];
  summary.getRange('B12').formulas = [[`=MEDIAN('ステージ一覧'!E7:E51)`]];
  summary.getRange('B13').formulas = [[`=MEDIAN('星スコア'!H6:H185)`]];
  styleTable(summary, 'A6:B13', 'A6:B6');
  summary.getRange('B7:B12').format.numberFormat = '0';
  summary.getRange('B13').format.numberFormat = '0.00x';
  summary.getRange('D6:E12').values = [
    ['読み方', '説明'],
    ['星1〜3', '通常キャンペーンの評価線。人数ごとに別設定。'],
    ['星4', '全厨房で星3達成後に解放されるNew Game+の評価線。'],
    ['料理点', '本表ではチップ前の料理本体点。推定式は「材料数×20＋工程複雑度加点」。'],
    ['時間', '加熱・混合等の秒数は公式仕様書ではなく上級者実測。フレームや入力遅延で小差が出る。'],
    ['盤面', '日本語欄は個別Wiki記述の要約。全文は「原文盤面記述」シートに保存。'],
    ['注意', 'Steam版・Switch/PS/Xbox・All You Can Eat間では星条件が違う場合がある。'],
  ];
  summary.getRange('D6:E6').format.fill = colors.blue;
  summary.getRange('D6:E6').format.font = { name: 'Aptos', size: 10, bold: true, color: colors.white };
  summary.getRange('D7:E12').format.font = { name: 'Aptos', size: 10, color: colors.ink };
  summary.getRange('D7:E12').format.wrapText = true;
  summary.getRange('D6:E12').format.borders = { preset: 'outside', style: 'thin', color: colors.grid };
  summary.getRange('A:A').format.columnWidth = 32;
  summary.getRange('B:B').format.columnWidth = 18;
  summary.getRange('C:C').format.columnWidth = 3;
  summary.getRange('D:D').format.columnWidth = 18;
  summary.getRange('E:E').format.columnWidth = 76;
  summary.getRange('F:H').format.columnWidth = 3;
  summary.getRange('7:12').format.rowHeight = 26;
  summary.tabColor = colors.navy;

  styleTitle(stages, '全45厨房：盤面・時間・料理', '位置関係と変化タイミングを実装参照用に圧縮。秒数列は計算・比較用の数値。', 'L');
  const stageHeaders = ['種別', '厨房', 'テーマ', '料理系統', 'ソロ秒', '協力秒', '表示時間', '盤面構成', '主要ギミック', '動的（Wiki）', '皿洗い', '出典URL'];
  const stageRows = data.records.map((record) => {
    const time = parseTimes(record);
    return [
      stageKind(record.stage), record.stage, record.stage === 'Tutorial' ? '—' : record.theme, jpRecipes(record.recipes),
      time.solo, time.coop, time.display, layoutJa[record.stage] || '',
      record.obstacles, record.dynamic, record.plates, record.url
    ];
  });
  stages.getRange('A6:L' + (6 + stageRows.length)).values = [stageHeaders, ...stageRows];
  styleTable(stages, 'A6:L51', 'A6:L6');
  stages.freezePanes.freezeRows(6);
  stages.freezePanes.freezeColumns(2);
  stages.getRange('A7:G51').format.verticalAlignment = 'center';
  stages.getRange('H7:L51').format.verticalAlignment = 'top';
  stages.getRange('H7:L51').format.wrapText = true;
  stages.getRange('E7:F51').format.numberFormat = '0';
  stages.getRange('A:A').format.columnWidth = 11;
  stages.getRange('B:B').format.columnWidth = 12;
  stages.getRange('C:C').format.columnWidth = 22;
  stages.getRange('D:D').format.columnWidth = 24;
  stages.getRange('E:F').format.columnWidth = 10;
  stages.getRange('G:G').format.columnWidth = 19;
  stages.getRange('H:H').format.columnWidth = 64;
  stages.getRange('I:I').format.columnWidth = 34;
  stages.getRange('J:K').format.columnWidth = 13;
  stages.getRange('L:L').format.columnWidth = 48;
  stages.getRange('7:51').format.rowHeight = 50;
  stages.tabColor = colors.navy;

  styleTitle(scores, '全45厨房：人数別星スコア', '1厨房×4人数を1行ずつ収録。星4はNew Game+条件。H列は比較用計算列。', 'H');
  const scoreRows = [];
  for (const record of data.records) {
    for (let player = 1; player <= 4; player += 1) {
      const values = record.scores[player + (player === 1 ? ' Player' : ' Players')];
      scoreRows.push([stageKind(record.stage), record.stage, player, ...values, null]);
    }
  }
  scores.getRange('A6:H' + (6 + scoreRows.length)).values = [
    ['種別', '厨房', '人数', '星1', '星2', '星3', '星4', '4★/3★'], ...scoreRows
  ];
  scores.getRange('H7').formulasR1C1 = [['=IFERROR(RC[-1]/RC[-2],0)']];
  scores.getRange('H7:H186').fillDown();
  styleTable(scores, 'A6:H186', 'A6:H6');
  scores.freezePanes.freezeRows(6);
  scores.freezePanes.freezeColumns(2);
  scores.getRange('C7:G186').format.numberFormat = '0';
  scores.getRange('H7:H186').format.numberFormat = '0.00x';
  scores.getRange('A:A').format.columnWidth = 11;
  scores.getRange('B:B').format.columnWidth = 13;
  scores.getRange('C:H').format.columnWidth = 11;
  scores.tabColor = colors.navy;

  styleTitle(recipeSheet, '料理34種：材料と基本点', '基本点はコミュニティ実測式から算出。チップは含まない。料理系統は11分類。', 'G');
  recipeSheet.getRange('A6:G' + (6 + recipes.length)).values = [
    ['系統', '完成料理', '材料', '材料数', '工程複雑度加点', '計算基本点', '確度'],
    ...recipes.map((r) => [...r, null, '中'])
  ];
  recipeSheet.getRange('F7').formulasR1C1 = [['=RC[-2]*20+RC[-1]']];
  recipeSheet.getRange('F7:F40').fillDown();
  styleTable(recipeSheet, 'A6:G40', 'A6:G6');
  recipeSheet.freezePanes.freezeRows(6);
  recipeSheet.getRange('C7:C40').format.wrapText = true;
  recipeSheet.getRange('D7:F40').format.numberFormat = '0';
  recipeSheet.getRange('A:A').format.columnWidth = 16;
  recipeSheet.getRange('B:B').format.columnWidth = 28;
  recipeSheet.getRange('C:C').format.columnWidth = 48;
  recipeSheet.getRange('D:F').format.columnWidth = 16;
  recipeSheet.getRange('G:G').format.columnWidth = 12;
  recipeSheet.tabColor = colors.blue;

  styleTitle(timingSheet, '工程別の所要時間', '本編食材は工程ごとの共通時間で動くため、器具別の基準値として整理。公式未公開の実測値。', 'G');
  timingSheet.getRange('A6:G' + (6 + timings.length)).values = [
    ['工程', '器具・対象', '該当する主な材料', '基準時間', '補足', '確度', '根拠'], ...timings
  ];
  styleTable(timingSheet, 'A6:G14', 'A6:G6');
  timingSheet.freezePanes.freezeRows(6);
  timingSheet.getRange('A7:G14').format.wrapText = true;
  timingSheet.getRange('A:A').format.columnWidth = 18;
  timingSheet.getRange('B:B').format.columnWidth = 20;
  timingSheet.getRange('C:C').format.columnWidth = 70;
  timingSheet.getRange('D:D').format.columnWidth = 28;
  timingSheet.getRange('E:E').format.columnWidth = 46;
  timingSheet.getRange('F:F').format.columnWidth = 12;
  timingSheet.getRange('G:G').format.columnWidth = 38;
  timingSheet.getRange('7:14').format.rowHeight = 42;
  timingSheet.tabColor = colors.blue;

  styleTitle(raw, '個別Wikiの盤面記述（原文）', '日本語要約で落とした設備位置・周期を再確認するための監査用原文。', 'C');
  raw.getRange('A6:C51').values = [
    ['厨房', '原文盤面記述', '出典URL'],
    ...data.records.map((r) => [r.stage, String(r.overview || '個別ページのKitchen節を参照').startsWith('=') ? "'" + r.overview : (r.overview || '個別ページのKitchen節を参照'), r.url])
  ];
  styleTable(raw, 'A6:C51', 'A6:C6');
  raw.freezePanes.freezeRows(6);
  raw.getRange('B7:C51').format.wrapText = true;
  raw.getRange('A:A').format.columnWidth = 14;
  raw.getRange('B:B').format.columnWidth = 110;
  raw.getRange('C:C').format.columnWidth = 50;
  raw.getRange('7:51').format.rowHeight = 78;
  raw.tabColor = colors.muted;

  styleTitle(sourceSheet, '出典と確度', '公式情報、コミュニティ集計、プレイヤー実測を分離。数値の用途に応じて確度を確認する。', 'E');
  sourceSheet.getRange('A6:E' + (6 + sources.length)).values = [
    ['区分', '資料名', 'URL', '使用箇所', '確度'], ...sources
  ];
  styleTable(sourceSheet, 'A6:E15', 'A6:E6');
  sourceSheet.getRange('A7:E15').format.wrapText = true;
  sourceSheet.getRange('A:A').format.columnWidth = 20;
  sourceSheet.getRange('B:B').format.columnWidth = 38;
  sourceSheet.getRange('C:C').format.columnWidth = 66;
  sourceSheet.getRange('D:D').format.columnWidth = 54;
  sourceSheet.getRange('E:E').format.columnWidth = 13;
  sourceSheet.getRange('7:15').format.rowHeight = 38;
  sourceSheet.tabColor = colors.muted;

  return wb;
}

function mdEscape(value) {
  return String(value ?? '').replaceAll('|', '\\|').replaceAll('\n', '<br>');
}

function scoreText(record, player) {
  return record.scores[player + (player === 1 ? ' Player' : ' Players')].join('/');
}

function makeReport(data) {
  const allRatios = [];
  const coopTimes = [];
  for (const record of data.records) {
    coopTimes.push(parseTimes(record).coop);
    for (let player = 1; player <= 4; player += 1) {
      const s = record.scores[player + (player === 1 ? ' Player' : ' Players')];
      allRatios.push(s[3] / s[2]);
    }
  }
  const timeCounts = new Map();
  for (const seconds of coopTimes) timeCounts.set(seconds, (timeCounts.get(seconds) || 0) + 1);
  const fourMinuteCount = timeCounts.get(240) || 0;
  const dynamicCount = data.records.filter((r) => String(r.dynamic).startsWith('Yes')).length;
  const lines = [];

  lines.push('# Overcooked! 2 本編：全厨房・時間・スコア・料理工程調査');
  lines.push('');
  lines.push('- 調査日: 2026-09-12');
  lines.push('- 対象: PC/Steam版を基準にしたベースキャンペーン45厨房（Tutorial 1、本編36、Kevin 8）');
  lines.push('- 対象外: DLC、季節アップデート、Arcade／Versus専用差分、All You Can Eat版の再調整値');
  lines.push('- 目的: COOKED OUT!のチュートリアル8面、出張営業、料理工程、人数別スコア設計の比較基準を得る');
  lines.push('');
  lines.push('## 結論');
  lines.push('');
  lines.push('1. 公式が数えるベースゲームは45厨房で、内訳はTutorial 1、本編36、Kevin 8である。[^1]');
  lines.push('2. 料理は、ゲーム内の工程ラベルとして分けると11系統・34完成品バリエーション。本家の量は「専用操作の多さ」より、同じ操作列に材料差と厨房分断を重ねることで生まれている。');
  lines.push('3. 協力時の公称制限時間中央値は' + median(coopTimes) + '秒。4分厨房が' + fourMinuteCount + '面で最多で、6-6だけはソロ25分／協力15分の長期総合試験である。');
  lines.push('4. WikiがDynamicと明記する大規模変形厨房は' + dynamicCount + '面だが、局所的な足場、階段、コンベア、風、車を含めれば、実際に盤面状態が変わる厨房はさらに多い。');
  lines.push('5. 星4条件は、全人数・全厨房の中央値で星3の約' + median(allRatios).toFixed(2) + '倍。単なる少し上の目標ではなく、注文順コンボと工程重畳を前提にした別難度である。New Game+は全厨房の星3達成後に解放される。[^2]');
  lines.push('');
  lines.push('## 数値の扱いと限界');
  lines.push('');
  lines.push('- 星条件は、1〜4人・星1〜4を同じ基準で網羅するSteamコミュニティ表を採用した。作者自身がPC版の値であり、他機種では異なる場合があると明記している。[^3]');
  lines.push('- 個別Wikiの星表とは一部で差がある。例としてTutorialの星4はSteam表が600/900/900/1250、個別Wikiが400/600/800/900である。本資料は比較可能性を優先しSteam表へ統一した。');
  lines.push('- 盤面構成と制限時間は個別ステージWikiの設備配置・周期記述を統合した。日本語欄は実装検討用の要約であり、正確な1マス単位座標図ではない。原文全文はExcelの「原文盤面記述」に保存した。');
  lines.push('- 調理秒数は公式パラメータ表が公開されていないため、上級者によるフレーム／ストップウォッチ実測を採用した。したがって1秒未満の小差は確定値として扱わない。[^4]');
  lines.push('- 料理の基本点は日本語Wikiの範囲と、プレイヤーが実測した「材料数×20＋工程複雑度加点」の式を突合した推定値。チップ前の値である。[^5][^6]');
  lines.push('');
  lines.push('## 全45厨房：盤面構成と制限時間');
  lines.push('');
  lines.push('| 厨房 | 時間 | 料理系統 | 盤面構成 | 主な妨害・変化 |');
  lines.push('|---|---:|---|---|---|');
  for (const record of data.records) {
    const time = parseTimes(record);
    lines.push('| [' + mdEscape(record.stage) + '](' + record.url + ') | ' + mdEscape(time.display) + ' | ' + mdEscape(jpRecipes(record.recipes)) + ' | ' + mdEscape(layoutJa[record.stage]) + ' | ' + mdEscape(record.obstacles) + ' |');
  }
  lines.push('');
  lines.push('### 盤面の難度上昇パターン');
  lines.push('');
  lines.push('- World 1は固定動線から始め、歩行者、コンベア、2厨房分断、大規模な墜落変形へ進む。新操作と地形変化を同時に増やしすぎない。');
  lines.push('- World 2〜3は「担当エリア分断＋受渡し」を中心に、可動台、回転カウンター、階段、ポータルへ発展する。');
  lines.push('- World 4〜5は、ベルト方向・風・ボタン・他者側の足場操作など、状態を読んで同期する仕組みを重ねる。');
  lines.push('- World 6は既出設備の複合で、6-6は料理注文そのものをトリガーに盤面フェーズを切り替える総合試験。');
  lines.push('- Kevin面は新しい蒸し料理を共通課題に固定し、盤面だけを大きく変えるため、純粋に連携構造を比較しやすい。');
  lines.push('');
  lines.push('## 全45厨房：人数別の星条件');
  lines.push('');
  lines.push('各セルは星1/星2/星3/星4。星4はNew Game+でのみ有効。');
  lines.push('');
  lines.push('| 厨房 | 1人 | 2人 | 3人 | 4人 |');
  lines.push('|---|---:|---:|---:|---:|');
  for (const record of data.records) {
    lines.push('| ' + mdEscape(record.stage) + ' | ' + scoreText(record, 1) + ' | ' + scoreText(record, 2) + ' | ' + scoreText(record, 3) + ' | ' + scoreText(record, 4) + ' |');
  }
  lines.push('');
  lines.push('### 星条件から分かること');
  lines.push('');
  lines.push('- 人数が増えても厨房の通路幅や設備数は同じため、人数別スコアは単純比例ではない。4人条件が3人と同じ、または低い厨房もある。');
  lines.push('- 星1は導線理解だけでも届く低い入口、星2〜3は安定した工程並列、星4は注文順コンボを切らさない最適化へ役割が分かれている。');
  lines.push('- 1-1やTutorialの星4は星3からの飛躍が特に大きい。導入面でもランキング的な詰めプレイが成立する、というCOOKED OUT!の方針と相性がよい。');
  lines.push('');
  lines.push('## スコア計算とチップ');
  lines.push('');
  lines.push('日本語コミュニティWikiでは、提供1品の得点を「基本スコア＋チップ×倍率」、最終得点を「提供した全料理の得点−時間切れ注文数×30」と整理している。チップは注文の残り時間に応じて3〜8点。[^5]');
  lines.push('');
  lines.push('| 直前までに順番通り提供した数 | 次の料理のチップ倍率 |');
  lines.push('|---:|---:|');
  lines.push('| 0〜1 | ×1 |');
  lines.push('| 2 | ×2 |');
  lines.push('| 3 | ×3 |');
  lines.push('| 4以上 | ×4 |');
  lines.push('');
  lines.push('順不同提供または注文失効でゲージはリセットされる。ただし順序を崩した料理自体には直前までの倍率が乗るため、最後の一皿だけは順不同でも後続への損失がない。公式も高得点には注文順コンボが鍵だと説明している。[^1]');
  lines.push('');
  lines.push('## 全34完成料理：材料とチップ前基本点');
  lines.push('');
  lines.push('| 系統 | 完成料理 | 材料 | 材料数 | 複雑度加点 | 推定基本点 |');
  lines.push('|---|---|---|---:|---:|---:|');
  for (const recipe of recipes) {
    const [family, name, ingredients, count, bonus] = recipe;
    lines.push('| ' + family + ' | ' + name + ' | ' + ingredients + ' | ' + count + ' | ' + bonus + ' | ' + (count * 20 + bonus) + ' |');
  }
  lines.push('');
  lines.push('この34種は、全ステージのRecipeテンプレート名を重複除去したもの。料理名の分類方法によって「系統数」は変わり得るが、本資料では工程が違うパンケーキとケーキを分け、揚げ物を1系統として11系統にした。');
  lines.push('');
  lines.push('## 材料・器具別の所要時間');
  lines.push('');
  lines.push('| 工程 | 対象材料 | 基準時間 | 補足 | 確度 |');
  lines.push('|---|---|---:|---|---|');
  for (const row of timings) {
    lines.push('| ' + mdEscape(row[0]) + ' | ' + mdEscape(row[2]) + ' | ' + mdEscape(row[3]) + ' | ' + mdEscape(row[4]) + ' | ' + row[5] + ' |');
  }
  lines.push('');
  lines.push('### 食材ごとの読み替え');
  lines.push('');
  lines.push('- 米・パスタ: 鍋で12秒。');
  lines.push('- 牛肉・鶏肉・キノコ・パスタ具: 必要なら切った後、フライパンで12秒。');
  lines.push('- 鶏肉・ジャガイモの揚げ物: 切った後、フライヤーで10秒。');
  lines.push('- ピザ・ケーキ: 必要材料を組立／混合した後、オーブンで10秒。');
  lines.push('- パンケーキ: 生地を12秒混合し、フライパンで12秒。');
  lines.push('- 団子: 材料を12秒混合し、蒸し器で12秒。蒸し魚は混合なし。');
  lines.push('- 直火器具は完成後約12秒、オーブン／フライヤーは完成後約10秒で焦げに到達する。実測ガイドでは「生→完成」と「完成→焦げ」が同時間とされる。[^4]');
  lines.push('- 全材料を最初から入れた混合は、2〜4材料でも約12〜12.5秒。途中投入では全体進捗が再計算される。[^7]');
  lines.push('');
  lines.push('## COOKED OUT!への反映');
  lines.push('');
  lines.push('1. チュートリアル8面では、1-1〜1-3に相当する固定盤面・鍋・投擲・配達を先に完了させ、1-4以降でコンベア、分断、皿洗い、火災、動的変形を一つずつ追加する。');
  lines.push('2. 初期料理量は「11系統を全部入れる」より、工程文法を網羅する系統を選び、各系統3〜4派生で注文識別を増やす。本家も34完成品を11系統へ圧縮できる。');
  lines.push('3. 通常営業の基本制限は3〜4分を中心に試験し、最終大型出張営業だけ長期フェーズ制にする。Core Alphaでは1面2分30秒〜4分10秒を上限にすると調整しやすい。');
  lines.push('4. 星1〜3は本編進行、競技スコアは別に扱う既決定と整合する。本家の星4のような2倍前後の急上昇を通常クリア条件へ混ぜない。');
  lines.push('5. 盤面ギミックは、背景の派手さより「誰が何へアクセスできるか」「中間物をどこで渡すか」「いつ接続状態が変わるか」の3変数で仕様書化する。');
  lines.push('6. 本家の数値は模倣値ではなく負荷の比較基準として使い、COOKED OUT!では自配達時間、共通容器、設備レベルの影響を含めた独自係数で再計測する。');
  lines.push('');
  lines.push('## Sources');
  lines.push('');
  lines.push('[^1]: [Team17 — The A-Z of Overcooked! 2](https://www.team17.com/news/the-a-z-of-overcooked-2)（45厨房、コンボ、Dynamic Kitchens）');
  lines.push('[^2]: [Team17 — Overcooked! 2 FAQ](https://www.team17.com/news/overcooked-2-faq)（New Game+と4つ星）');
  lines.push('[^3]: [Steam Community — Score Requirements for Every Level](https://steamcommunity.com/sharedfiles/filedetails/?id=1832823209)（PC版の人数別星条件）');
  lines.push('[^4]: [Steam Community — OC2/AYCE Tech v2.2](https://steamcommunity.com/sharedfiles/filedetails/?id=2456165919)（切断・混合・加熱・焦げ猶予の実測）');
  lines.push('[^5]: [overcooked2 @ ウィキ — スコア](https://w.atwiki.jp/overcooked2/pages/18.html)（チップ、倍率、失注ペナルティ）');
  lines.push('[^6]: [Reddit — OC2 Scoring Calculations](https://www.reddit.com/r/OvercookedGame/comments/lt1o1g/)（基本点の実測式）');
  lines.push('[^7]: [Reddit — Cooking / Mixing Times Analysis](https://www.reddit.com/r/OvercookedGame/comments/c66t2v/)（混合時間と途中投入の実測）');
  lines.push('');
  return lines.join('\n');
}

async function main() {
  const data = JSON.parse(await fs.readFile(dataPath, 'utf8'));
  if (data.record_count !== 45) throw new Error('Expected 45 stage records');
  for (const record of data.records) {
    if (!layoutJa[record.stage]) throw new Error('Missing Japanese layout summary: ' + record.stage);
    if (Object.keys(record.scores).length !== 4) throw new Error('Incomplete score rows: ' + record.stage);
  }

  await fs.mkdir(outputDir, { recursive: true });
  await fs.mkdir(previewDir, { recursive: true });
  await fs.writeFile(reportPath, makeReport(data), 'utf8');

  const wb = makeWorkbook(data);
  await wb.recalculate();

  const inspections = [];
  for (const [sheetName, range] of [
    ['概要', 'A1:H14'], ['ステージ一覧', 'A1:L51'], ['星スコア', 'A1:H186'],
    ['料理・基本点', 'A1:G40'], ['工程時間', 'A1:G14'],
    ['原文盤面記述', 'A1:C51'], ['出典', 'A1:E15']
  ]) {
    inspections.push(await wb.inspect({ kind: 'region', sheetId: sheetName, range, maxChars: 1800 }));
    const preview = await wb.render({ sheetName, autoCrop: 'all', scale: 0.9, format: 'png' });
    await fs.writeFile(path.join(previewDir, sheetName + '.png'), new Uint8Array(await preview.arrayBuffer()));
  }
  const formulaCheck = await wb.inspect({
    kind: 'match',
    searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A',
    options: { useRegex: true, maxResults: 100 },
    maxChars: 3000
  });
  console.log('Formula check:', formulaCheck.ndjson || formulaCheck);
  console.log('Inspected sheets:', inspections.length);

  const output = await SpreadsheetFile.exportXlsx(wb);
  await output.save(workbookPath);
  const saved = await FileBlob.load(workbookPath);
  const reopened = await SpreadsheetFile.importXlsx(saved);
  const reopenedSummary = await reopened.inspect({ kind: 'region', sheetId: '概要', range: 'A6:B13', maxChars: 1800 });
  const reopenedErrors = await reopened.inspect({
    kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A',
    options: { useRegex: true, maxResults: 100 }, maxChars: 1800
  });
  console.log('Reopened summary:', reopenedSummary.ndjson || reopenedSummary);
  console.log('Reopened formula check:', reopenedErrors.ndjson || reopenedErrors);
  console.log(workbookPath);
  console.log(reportPath);
  console.log(previewDir);
}

await main();
