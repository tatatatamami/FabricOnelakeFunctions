# Contributing Guide

�{�v���W�F�N�g�ւ悤�����BAzure Functions (.NET 8 / Isolated Worker) ��p�����T�[�o�[���X�@�\�Q�ł��B�J�����̏������� Issue / Pull Request �̊�{���[�����ȉ��ɂ܂Ƃ߂܂��B

---
## 1. �v���W�F�N�g�O��
- �����^�C��: Azure Functions v4 (Isolated Worker)
- �^�[�Q�b�g�t���[�����[�N: .NET 8
- ����: C# 12
- ���[�J�����s: Azure Functions Core Tools

---
## 2. �J�����Z�b�g�A�b�v
### 2-1. �K�v�c�[��
| �c�[�� | �ړI | �Q�l |
|--------|------|------|
| .NET 8 SDK | �r���h / ���s | https://dotnet.microsoft.com/ |
| Azure Functions Core Tools v4 | Functions ���[�J�����s | https://learn.microsoft.com/azure/azure-functions/functions-run-local |
| Azure CLI | �F�� / �f�v���C | https://learn.microsoft.com/cli/azure/ |
| (�C��) Visual Studio / VS Code | �J�� | VS Code �� Azure Functions �g������ |

### 2-2. ���|�W�g���擾
```
git clone <your-fork-or-this-repo-url>
cd function-onelake
```

### 2-3. �ˑ�����
```
dotnet restore
```

### 2-4. Azure CLI ���O�C��
```
az login
```
�K�v�ɉ����ăT�u�X�N���v�V������I��:
```
az account set --subscription <SUBSCRIPTION_ID_OR_NAME>
```

### 2-5. local.settings.json �쐬
`local.settings.json` �� Git �Ǘ��O�ł� (�铽�����܂މ\�������邽��)�B�ȉ��̃e���v���[�g���v���W�F�N�g���[�g (csproj �Ɠ��K�w) �ɍ쐬���Ă��������B
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ONELAKE_DFS_FILE_URL": "https://<workspace>.dfs.fabric.microsoft.com/<path>",
    "SQL_ENDPOINT": "sql.<workspace>.fabric.microsoft.com",
    "SQL_DATABASE": "<database-name>"
  }
}
```

### 2-6. �ݒ�L�[�ꗗ
| �L�[ | ���� | �� |
|------|------|----|
| ONELAKE_DFS_FILE_URL | OneLake ���� Data Lake / �t�@�C���Q�� URL | https://contoso.dfs.fabric.microsoft.com/Files/sample.csv |
| SQL_ENDPOINT | Fabric SQL Endpoint �z�X�g�� | sql.contoso.fabric.microsoft.com |
| SQL_DATABASE | �ڑ���f�[�^�x�[�X�� | AnalyticsDb |

### 2-7. ���[�J�����s
```
func start
```
�܂��� (�f�o�b�O�p):
```
dotnet build
func start
```
`http://localhost:7071/api/Function1` ���u���E�U / curl �Ŋm�F�B

---
## 3. �u�����`�^�p (����)
- main: ��Ƀf�v���C�\ / ����
- feature/*: �@�\�ǉ�
- fix/*: �o�O�C��
- chore/*: �ݒ� / �����e�i���X

---
## 4. Issue �쐬���[��
Issue �e���v���͈ȉ� 3 �u���b�N����{�Ƃ��܂��B
1. �w�i (�Ȃ��K�v�� / ����̉ۑ�)
2. �v�� (�ӏ������ŋ@�\ / �ύX�͈�)
3. �󂯓������ (�e�X�g�ϓ_ / �������� / �z�胊�N�G�X�g�� �Ȃ�)

�ǉ�����: �X�N���[���V���b�g / �Q�l�����N / �z���@�\�v���B

---
## 5. Pull Request ���[��
| ���� | ���[�� |
|------|--------|
| �P�� | 1 PR = 1 �@�\ / 1 �_���I�ύX (��剻�����Ȃ�) |
| Issue �R�t�� | `Closes #<Issue�ԍ�>` �� PR �����Ɋ܂߂� |
| �R�~�b�g | �Ӗ��̂��闱�x / Imperative �ŉp�ꐄ�� (��: Add X, Fix Y) |
| �e���v�� | `.github/pull_request_template.md` (���݂���ꍇ) �𗘗p |
| ���r���[ | �Œ� 1 �����F (�����e�X�g������̓O���[���K�{) |
| ����m�F | ���[�J�����s�Ŋ�{��������؂��A�Č��菇���L�� |

### PR ���������\��
- Summary
- Related Issue: `Closes #123`
- Changes
- How to Test
- Screenshots / Logs (�K�v�ɉ���)
- Notes / Breaking Changes

---
## 6. �R�[�f�B���O�w�j (�ŏ�)
- ���O: `ILogger` �ō\�������b�Z�[�W���g�p (`_logger.LogInformation("Processing {File}", fileName);` �Ȃ�)
- �ݒ�l: ���ˑ��l�͊��ϐ� / local.settings.json Values ����擾
- ��O: �\�����Ȃ���O�͕ߑ��������s�����A�K�v�Ȃ�ăX���[�O�Ƀ��O

---
## 7. �Z�L�����e�B / �V�[�N���b�g
- API �L�[ / �ڑ�������̓R�~�b�g�֎~
- �T���v���̓_�~�[�l���g�p
- `local.settings.json` ������ăR�~�b�g���Ȃ� (���� .gitignore �Ɋ܂܂�Ă��邩�m�F)

---
## 8. �悭����g���u��
| ���� | �Ώ� |
|------|------|
| `func start` �� Storage �֘A�G���[ | `AzureWebJobsStorage` �̒l�����[�J���J���p���m�F (`UseDevelopmentStorage=true`) |
| ���s�|�[�g���� | `--port 7072` �ȂǂŕύX |
| �F�؎��s | `az login` ��� `az account show` �ŃT�u�X�N���m�F |

---
## 9. �����[�X (�����g���p)
CI/CD �ǉ����: �r���h / �e�X�g / �f�v���C�菇�������ɒǋL�B

---
�����s��������� Issue ���쐬���Ă��������BHappy Coding!
