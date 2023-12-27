if object_id('dbo.Workers') is null 
begin 
	create table dbo.Workers 
		(id int not null identity(1, 1), 
		name varchar(255) not null, 
		host varchar(255) not null, 
		appName varchar(512) not null default '', 
		date datetime not null, 
		settings varchar(2000) not null, 
		active bit not null, 
		executed datetime, 
		hosts int); 
	alter table dbo.Workers 
		add constraint Workers__PK primary key (id); 
	alter table dbo.Workers 
		add constraint Workers__UK unique (name, host, appName); 
end