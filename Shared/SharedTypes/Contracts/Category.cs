namespace SharedTypes.Contracts;

//
// ----- DTO -----
//

/// <summary>
/// DTO категории
/// </summary>
public sealed record CategoryDto
(
    long Id,
    string Name,
    bool IsSystem
);

//
// ----- RPC -----
//

/// <summary>
/// Запрос на получение категорий пользователя
/// </summary>
public sealed record GetCategoriesRequest
(
    long UserId
);

/// <summary>
/// Ответ на запрос на получение категорий пользователя
/// </summary>
public sealed record GetCategoriesResponse
(
    IReadOnlyList<CategoryDto> Categories
);

/// <summary>
/// Запрос на добавление пользовательской категории
/// </summary>
public sealed record AddCategoryRequest
(
    long UserId,
    string Name,
    string RequestId
);

/// <summary>
/// Ответ на запрос на добавление пользовательской категории
/// </summary>
public sealed record AddCategoryResponse
(
    bool Success
);

/// <summary>
/// Запрос на удаление пользовательской категории
/// </summary>
public sealed record DeleteCategoryRequest
(
    long UserId,
    long CategoryId,
    string RequestId
);

/// <summary>
/// Ответ на запрос на удаление пользовательской категории
/// </summary>
public sealed record DeleteCategoryResponse
(
    bool Success
);

/// <summary>
/// Оповещение о том, что категории пользователя изменились (добавлена/удалена категория)
/// </summary>
public sealed record UserCategoriesChangedNotification
(
    long UserId,
    IReadOnlyList<CategoryDto> Categories
);
